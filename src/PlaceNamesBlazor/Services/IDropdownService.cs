using PlaceNamesBlazor.Contracts.Dropdowns;

namespace PlaceNamesBlazor.Services;

public interface IDropdownService
{
    Task<DropdownsDto> GetDropdownsAsync(CancellationToken cancellationToken = default);
}
