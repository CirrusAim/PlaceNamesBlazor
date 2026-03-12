using PlaceNamesBlazor.Contracts.Reporting;

namespace PlaceNamesBlazor.Services.Reporting;

public interface IReportingService
{
    Task<IReadOnlyList<UsagePeriodOptionDto>> GetUsagePeriodsForStampAsync(int stempelId, CancellationToken cancellationToken = default);
    Task<int?> SubmitReportAsync(int rapportoerId, ReportSubmitRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportDto>> GetReportsAsync(int? rapportoerId, int? stempelId, string? status, int? limit, int offset = 0, CancellationToken cancellationToken = default);
    Task<int> GetReportsCountAsync(int? rapportoerId, int? stempelId, string? status, CancellationToken cancellationToken = default);
    Task<int> GetPendingReportCountAsync(CancellationToken cancellationToken = default);
    Task<ReportDto?> GetReportByIdAsync(int rapporteringshistorikkId, CancellationToken cancellationToken = default);
    Task<bool> ApproveReportAsync(int rapporteringshistorikkId, string initialerBeslutter, string? kommentarer, int actorUserId, CancellationToken cancellationToken = default);
    Task<bool> RejectReportAsync(int rapporteringshistorikkId, string initialerBeslutter, int actorUserId, CancellationToken cancellationToken = default);
}
