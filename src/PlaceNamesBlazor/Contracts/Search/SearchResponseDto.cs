using PlaceNamesBlazor.Contracts.Common;

namespace PlaceNamesBlazor.Contracts.Search;

/// <summary>Result of stamp search. When guest limit is exceeded, Result is null and LimitExceeded is true.</summary>
public class SearchResponseDto
{
    public bool LimitExceeded { get; set; }
    public string? LimitExceededMessage { get; set; }
    public int? GuestSearchUsed { get; set; }
    public int? GuestSearchLimit { get; set; }
    public PagedResultDto<SearchResultDto>? Result { get; set; }
}
