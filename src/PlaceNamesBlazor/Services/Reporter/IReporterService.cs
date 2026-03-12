using PlaceNamesBlazor.Contracts.Reporter;

namespace PlaceNamesBlazor.Services.Reporter;

public interface IReporterService
{
    Task<ReporterDto?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ReporterDto?> GetByIdAsync(int rapportoerId, CancellationToken cancellationToken = default);
    Task<int?> RegisterAsync(int userId, ReporterRegisterRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int rapportoerId, string? initialer, string? fornavnEtternavn, string? epost, string? telefon, string? medlemsklubb, CancellationToken cancellationToken = default);
    Task<bool> ApproveAsync(int rapportoerId, CancellationToken cancellationToken = default);
    Task<bool> RejectAsync(int rapportoerId, CancellationToken cancellationToken = default);
}
