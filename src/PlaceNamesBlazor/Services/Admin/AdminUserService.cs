using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Contracts.Admin;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Data.Entities;
using PlaceNamesBlazor.Services.Audit;

namespace PlaceNamesBlazor.Services.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly PlaceNamesDbContext _db;
    private readonly IAuditService _audit;

    public AdminUserService(PlaceNamesDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<int> GetPendingReporterCountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Rapportoer
            .AsNoTracking()
            .CountAsync(r => (r.Status ?? "") == "pending", cancellationToken);
    }

    public async Task<IReadOnlyList<UserListDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive || !u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
        var result = new List<UserListDto>();
        foreach (var u in users)
        {
            Rapportoer? r = null;
            if (u.RapportoerId.HasValue)
                r = await _db.Rapportoer.AsNoTracking().FirstOrDefaultAsync(x => x.RapportoerId == u.RapportoerId.Value, cancellationToken);
            result.Add(ToListDto(u, r));
        }
        var approvedUserIds = result.Where(x => x.ReporterStatus == "approved").Select(x => x.UserId).ToList();
        if (approvedUserIds.Count > 0)
        {
            var approverEmails = await _audit.GetApproverEmailsForReportersAsync(approvedUserIds, cancellationToken);
            foreach (var dto in result)
                if (dto.ReporterStatus == "approved" && approverEmails.TryGetValue(dto.UserId, out var email))
                    dto.ApprovedByEmail = email;
        }
        return result;
    }

    public async Task<UserListDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (u == null) return null;
        Rapportoer? r = null;
        if (u.RapportoerId.HasValue)
            r = await _db.Rapportoer.AsNoTracking().FirstOrDefaultAsync(x => x.RapportoerId == u.RapportoerId.Value, cancellationToken);
        return ToListDto(u, r);
    }

    public async Task<bool> UpdateUserAsync(int userId, UserUpdateRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null) return false;

        var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == actorUserId, cancellationToken);
        var actorEmail = actor?.Email ?? "";
        var actorRole = actor?.Role ?? "";

        var changes = new Dictionary<string, (string? Old, string? New)>();
        void Track(string name, string? oldVal, string? newVal)
        {
            var n = newVal?.Trim();
            var o = oldVal?.Trim();
            if (n != o) changes[name] = (oldVal ?? "(empty)", newVal ?? "(empty)");
        }

        if (request.FirstName != null) { Track("first_name", user.FirstName, request.FirstName); user.FirstName = request.FirstName.Trim(); }
        if (request.LastName != null) { Track("last_name", user.LastName, request.LastName); user.LastName = request.LastName.Trim(); }
        if (request.MiddleName != null) { Track("middle_name", user.MiddleName, request.MiddleName); user.MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(); }
        if (request.Email != null)
        {
            var newEmail = request.Email.Trim().ToLowerInvariant();
            if (newEmail != user.Email)
            {
                if (await _db.Users.AnyAsync(u => u.Email == newEmail && u.UserId != userId, cancellationToken))
                    return false;
                Track("email", user.Email, newEmail);
                user.Email = newEmail;
            }
        }
        if (request.Telephone != null) { Track("telephone", user.Telephone, request.Telephone); user.Telephone = string.IsNullOrWhiteSpace(request.Telephone) ? null : request.Telephone.Trim(); }
        if (request.Username != null)
        {
            var newUsername = string.IsNullOrWhiteSpace(request.Username) ? null : request.Username.Trim();
            if (newUsername != user.Username)
            {
                if (newUsername != null && await _db.Users.AnyAsync(u => u.Username == newUsername && u.UserId != userId, cancellationToken))
                    return false;
                Track("username", user.Username, newUsername);
                user.Username = newUsername;
            }
        }

        if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName) || string.IsNullOrWhiteSpace(user.Email))
            return false;

        await _db.SaveChangesAsync(cancellationToken);
        if (changes.Count > 0)
        {
            var desc = $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim();
            var details = System.Text.Json.JsonSerializer.Serialize(new { email = user.Email, field_changes = changes });
            await _audit.LogAsync(actorUserId, actorEmail, actorRole, "user_updated", "USER", userId, desc, details, cancellationToken: cancellationToken);
        }
        return true;
    }

    public async Task<bool> DeleteUserAsync(int userId, int actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == actorUserId, cancellationToken);
        if (actor?.Role?.ToLowerInvariant() != "admin") return false;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null) return false;
        if (user.Role?.ToLowerInvariant() == "admin") return false;
        if (user.UserId == actorUserId) return false;

        var adminCount = await _db.Users.CountAsync(u => u.Role == "admin" && u.IsActive, cancellationToken);
        if (adminCount <= 1 && user.Role == "admin") return false;

        var desc = $"{user.FirstName} {user.MiddleName} {user.LastName}".Trim();
        user.IsActive = false;
        if (user.RapportoerId.HasValue)
        {
            var rep = await _db.Rapportoer.FirstOrDefaultAsync(r => r.RapportoerId == user.RapportoerId.Value, cancellationToken);
            if (rep != null) rep.Status = "deactivated";
        }
        user.RapportoerId = null;
        await _db.SaveChangesAsync(cancellationToken);

        var details = System.Text.Json.JsonSerializer.Serialize(new { email = user.Email, role = user.Role });
        await _audit.LogAsync(actorUserId, actor.Email, actor.Role ?? "", "user_deleted", "USER", userId, desc, details, cancellationToken: cancellationToken);
        return true;
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, string role, int actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == actorUserId, cancellationToken);
        if (actor?.Role?.ToLowerInvariant() != "admin") return false;

        if (role?.ToLowerInvariant() == "admin") return false;
        if (role != "registered" && role != "superuser") return false;

        var target = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (target == null) return false;
        if (target.Role?.ToLowerInvariant() == "admin") return false;
        var oldRole = target.Role;
        target.Role = role;
        await _db.SaveChangesAsync(cancellationToken);

        var actionType = role == "superuser" ? "role_promoted" : "role_revoked";
        var desc = $"{target.FirstName} {target.MiddleName} {target.LastName}".Trim();
        var details = System.Text.Json.JsonSerializer.Serialize(new { email = target.Email, old_role = oldRole, new_role = role });
        await _audit.LogAsync(actorUserId, actor?.Email ?? "", actor?.Role ?? "", actionType, "USER", userId, desc, details, cancellationToken: cancellationToken);
        return true;
    }

    private static UserListDto ToListDto(User u, Rapportoer? r)
    {
        return new UserListDto
        {
            UserId = u.UserId,
            FirstName = u.FirstName,
            LastName = u.LastName,
            MiddleName = u.MiddleName,
            Email = u.Email,
            Role = u.Role ?? "",
            Username = u.Username,
            Telephone = u.Telephone,
            IsActive = u.IsActive,
            LastLogin = u.LastLogin,
            CreatedAt = u.CreatedAt,
            RapportoerId = u.RapportoerId,
            ReporterStatus = r?.Status,
            ReporterInitialer = r?.Initialer,
            ReporterFornavnEtternavn = r?.FornavnEtternavn
        };
    }
}
