using PlaceNamesBlazor.Contracts.Search;

namespace PlaceNamesBlazor.Services;

public interface IStampSearchService
{
    Task<SearchResponseDto> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<RecordDetailDto?> GetByIdAsync(int stempelId, CancellationToken cancellationToken = default);
}
