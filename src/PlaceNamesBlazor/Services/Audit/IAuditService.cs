namespace PlaceNamesBlazor.Services.Audit;

public interface IAuditService
{
    Task LogAsync(int actorId, string actorEmail, string actorRole, string actionType, string? targetType = null, int? targetId = null, string? targetDescription = null, string? detailsJson = null, string? ipAddress = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogEntryDto>> GetLogsAsync(string? actionType = null, string? actorFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, int limit = 1000, int offset = 0, CancellationToken cancellationToken = default);
    Task<int> GetLogsCountAsync(string? actionType = null, string? actorFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, string>> GetApproverEmailsForReportersAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default);
}

public class AuditLogEntryDto
{
    public int AuditId { get; set; }
    public int ActorId { get; set; }
    public string ActorEmail { get; set; } = "";
    public string ActorRole { get; set; } = "";
    public string ActionType { get; set; } = "";
    public string? TargetType { get; set; }
    public int? TargetId { get; set; }
    public string? TargetDescription { get; set; }
    public string? DetailsJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}
