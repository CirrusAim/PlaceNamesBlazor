using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Data.Entities;

namespace PlaceNamesBlazor.Services.Audit;

public class AuditService : IAuditService
{
    private readonly PlaceNamesDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(PlaceNamesDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(int actorId, string actorEmail, string actorRole, string actionType, string? targetType = null, int? targetId = null, string? targetDescription = null, string? detailsJson = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var ctx = _httpContextAccessor.HttpContext;
        ipAddress ??= ctx?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = ctx?.Request?.Headers?.UserAgent.ToString();
        if (userAgent != null && userAgent.Length > 512)
            userAgent = userAgent[..512];
        var entry = new AuditLog
        {
            ActorId = actorId,
            ActorEmail = actorEmail,
            ActorRole = actorRole,
            ActionType = actionType,
            TargetType = targetType,
            TargetId = targetId,
            TargetDescription = targetDescription != null && targetDescription.Length > 255 ? targetDescription[..255] : targetDescription,
            DetailsJson = detailsJson, // Callers must not include passwords, tokens, or full request bodies
            IpAddress = ipAddress != null && ipAddress.Length > 45 ? ipAddress[..45] : ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogEntryDto>> GetLogsAsync(string? actionType = null, string? actorFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, int limit = 1000, int offset = 0, CancellationToken cancellationToken = default)
    {
        if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated != true ||
            !_httpContextAccessor.HttpContext.User.IsInRole("admin"))
            return Array.Empty<AuditLogEntryDto>();

        var q = ApplyLogsFilters(_db.AuditLogs.AsNoTracking(), actionType, actorFilter, dateFrom, dateTo);
        var list = await q.OrderByDescending(x => x.CreatedAt).Skip(offset).Take(limit).Select(x => new AuditLogEntryDto
        {
            AuditId = x.AuditId,
            ActorId = x.ActorId,
            ActorEmail = x.ActorEmail,
            ActorRole = x.ActorRole,
            ActionType = x.ActionType,
            TargetType = x.TargetType,
            TargetId = x.TargetId,
            TargetDescription = x.TargetDescription,
            DetailsJson = x.DetailsJson,
            IpAddress = x.IpAddress,
            UserAgent = x.UserAgent,
            CreatedAt = x.CreatedAt
        }).ToListAsync(cancellationToken);
        return list;
    }

    public async Task<int> GetLogsCountAsync(string? actionType = null, string? actorFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default)
    {
        if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated != true ||
            !_httpContextAccessor.HttpContext.User.IsInRole("admin"))
            return 0;
        var q = ApplyLogsFilters(_db.AuditLogs.AsNoTracking(), actionType, actorFilter, dateFrom, dateTo);
        return await q.CountAsync(cancellationToken);
    }

    private static IQueryable<AuditLog> ApplyLogsFilters(IQueryable<AuditLog> q, string? actionType, string? actorFilter, DateTime? dateFrom, DateTime? dateTo)
    {
        if (!string.IsNullOrWhiteSpace(actionType))
        {
            var at = actionType.Trim();
            q = q.Where(x => EF.Functions.ILike(x.ActionType, at));
        }
        if (!string.IsNullOrWhiteSpace(actorFilter))
        {
            var term = $"%{actorFilter.Trim()}%";
            q = q.Where(x =>
                EF.Functions.ILike(x.ActorEmail, term) ||
                EF.Functions.ILike(x.ActorRole, term) ||
                (x.TargetDescription != null && EF.Functions.ILike(x.TargetDescription, term)));
        }
        if (dateFrom.HasValue)
        {
            var fromUtc = ToUtcStartOfDay(dateFrom.Value);
            q = q.Where(x => x.CreatedAt >= fromUtc);
        }
        if (dateTo.HasValue)
        {
            var toUtc = ToUtcEndOfDay(dateTo.Value);
            q = q.Where(x => x.CreatedAt <= toUtc);
        }
        return q;
    }

    public async Task<IReadOnlyDictionary<int, string>> GetApproverEmailsForReportersAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds == null || userIds.Count == 0)
            return new Dictionary<int, string>();
        var list = await _db.AuditLogs
            .AsNoTracking()
            .Where(x => x.ActionType == "reporter_approved" && x.TargetType == "USER" && x.TargetId != null && userIds.Contains(x.TargetId.Value))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.TargetId, x.ActorEmail })
            .ToListAsync(cancellationToken);
        var dict = new Dictionary<int, string>();
        foreach (var item in list)
            if (item.TargetId.HasValue && !string.IsNullOrEmpty(item.ActorEmail) && !dict.ContainsKey(item.TargetId.Value))
                dict[item.TargetId.Value] = item.ActorEmail;
        return dict;
    }

    /// <summary>Date filter values must be UTC for Npgsql timestamp with time zone.</summary>
    private static DateTime ToUtcStartOfDay(DateTime d)
        => new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime ToUtcEndOfDay(DateTime d)
        => new DateTime(d.Year, d.Month, d.Day, 23, 59, 59, 999, DateTimeKind.Utc);
}
