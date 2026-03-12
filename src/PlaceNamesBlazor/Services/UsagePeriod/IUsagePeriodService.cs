using PlaceNamesBlazor.Contracts.UsagePeriod;

namespace PlaceNamesBlazor.Services.UsagePeriod;

public interface IUsagePeriodService
{
    Task<IReadOnlyList<UsagePeriodDto>> GetForStampAsync(int stempelId, CancellationToken cancellationToken = default);
    Task<(bool Success, int? BruksperiodeId, string? Error)> CreateAsync(int stempelId, UsagePeriodCreateRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(int bruksperiodeId, UsagePeriodUpdateRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int bruksperiodeId, CancellationToken cancellationToken = default);
}
