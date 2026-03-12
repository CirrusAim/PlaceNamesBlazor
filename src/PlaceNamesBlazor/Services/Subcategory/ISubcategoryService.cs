using PlaceNamesBlazor.Contracts.Subcategory;

namespace PlaceNamesBlazor.Services.Subcategory;

public interface ISubcategoryService
{
    Task<IReadOnlyList<SubcategoryDto>> GetByStempeltypeAsync(int stempeltypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubcategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, int? UnderkategoriId, string? Error)> CreateAsync(SubcategoryCreateRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(int underkategoriId, SubcategoryUpdateRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int underkategoriId, CancellationToken cancellationToken = default);
}
