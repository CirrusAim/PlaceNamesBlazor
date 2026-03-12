using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Contracts.Reporter;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Data.Entities;

namespace PlaceNamesBlazor.Services.Reporter;

public class ReporterService : IReporterService
{
    private readonly PlaceNamesDbContext _db;

    public ReporterService(PlaceNamesDbContext db)
    {
        _db = db;
    }

    public async Task<ReporterDto?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user?.RapportoerId == null) return null;
        return await GetByIdAsync(user.RapportoerId.Value, cancellationToken);
    }

    public async Task<ReporterDto?> GetByIdAsync(int rapportoerId, CancellationToken cancellationToken = default)
    {
        var r = await _db.Rapportoer.AsNoTracking().FirstOrDefaultAsync(x => x.RapportoerId == rapportoerId, cancellationToken);
        return r == null ? null : ToDto(r);
    }

    public async Task<int?> RegisterAsync(int userId, ReporterRegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null) return null;
        if (user.Role?.ToLowerInvariant() == "admin") return null;

        var initialer = request.Initialer.Trim();
        var fornavnEtternavn = request.FornavnEtternavn.Trim();
        var epost = request.Epost.Trim().ToLowerInvariant();

        if (user.RapportoerId != null)
        {
            var existing = await _db.Rapportoer.FirstOrDefaultAsync(x => x.RapportoerId == user.RapportoerId, cancellationToken);
            if (existing != null && existing.Status != "rejected")
                return null;
            if (existing != null && existing.Status == "rejected")
            {
                existing.Initialer = initialer;
                existing.FornavnEtternavn = fornavnEtternavn;
                existing.Epost = epost;
                existing.Telefon = string.IsNullOrWhiteSpace(request.Telefon) ? null : request.Telefon.Trim();
                existing.Medlemsklubb = string.IsNullOrWhiteSpace(request.Medlemsklubb) ? null : request.Medlemsklubb.Trim();
                existing.Status = "pending";
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return existing.RapportoerId;
            }
        }

        var existingByInitials = await _db.Rapportoer.FirstOrDefaultAsync(x => x.Initialer == initialer, cancellationToken);
        if (existingByInitials != null)
        {
            var linkedUser = await _db.Users.FirstOrDefaultAsync(u => u.RapportoerId == existingByInitials.RapportoerId, cancellationToken);
            if (linkedUser != null)
                return null;
        }

        var reporter = new Rapportoer
        {
            Initialer = initialer,
            FornavnEtternavn = fornavnEtternavn,
            Epost = epost,
            Telefon = string.IsNullOrWhiteSpace(request.Telefon) ? null : request.Telefon.Trim(),
            Medlemsklubb = string.IsNullOrWhiteSpace(request.Medlemsklubb) ? null : request.Medlemsklubb.Trim(),
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };
        _db.Rapportoer.Add(reporter);
        await _db.SaveChangesAsync(cancellationToken);
        user.RapportoerId = reporter.RapportoerId;
        await _db.SaveChangesAsync(cancellationToken);
        return reporter.RapportoerId;
    }

    public async Task<bool> UpdateAsync(int rapportoerId, string? initialer, string? fornavnEtternavn, string? epost, string? telefon, string? medlemsklubb, CancellationToken cancellationToken = default)
    {
        var r = await _db.Rapportoer.FirstOrDefaultAsync(x => x.RapportoerId == rapportoerId, cancellationToken);
        if (r == null) return false;
        if (initialer != null) r.Initialer = initialer;
        if (fornavnEtternavn != null) r.FornavnEtternavn = fornavnEtternavn;
        if (epost != null) r.Epost = epost;
        if (telefon != null) r.Telefon = telefon;
        if (medlemsklubb != null) r.Medlemsklubb = medlemsklubb;
        r.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ApproveAsync(int rapportoerId, CancellationToken cancellationToken = default)
    {
        var r = await _db.Rapportoer.FirstOrDefaultAsync(x => x.RapportoerId == rapportoerId, cancellationToken);
        if (r == null) return false;
        r.Status = "approved";
        r.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RejectAsync(int rapportoerId, CancellationToken cancellationToken = default)
    {
        var r = await _db.Rapportoer.FirstOrDefaultAsync(x => x.RapportoerId == rapportoerId, cancellationToken);
        if (r == null) return false;
        r.Status = "rejected";
        r.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ReporterDto ToDto(Rapportoer r)
    {
        return new ReporterDto
        {
            RapportoerId = r.RapportoerId,
            Initialer = r.Initialer,
            FornavnEtternavn = r.FornavnEtternavn,
            Epost = r.Epost,
            Telefon = r.Telefon,
            Medlemsklubb = r.Medlemsklubb,
            Status = r.Status ?? "pending",
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}
