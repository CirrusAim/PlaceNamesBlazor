using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Contracts.Subcategory;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Data.Entities;

namespace PlaceNamesBlazor.Services.Subcategory;

public class SubcategoryService : ISubcategoryService
{
    private readonly PlaceNamesDbContext _db;

    public SubcategoryService(PlaceNamesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SubcategoryDto>> GetByStempeltypeAsync(int stempeltypeId, CancellationToken cancellationToken = default)
    {
        return await _db.UnderkategoriStempeltyper
            .AsNoTracking()
            .Where(u => u.StempeltypeId == stempeltypeId)
            .OrderBy(u => u.Underkategori)
            .Select(u => new SubcategoryDto
            {
                UnderkategoriId = u.UnderkategoriId,
                StempeltypeId = u.StempeltypeId,
                Underkategori = u.Underkategori,
                UnderkategoriFullTekst = u.UnderkategoriFullTekst
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubcategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.UnderkategoriStempeltyper
            .AsNoTracking()
            .Include(u => u.Stempeltype)
            .OrderBy(u => u.Stempeltype!.Hovedstempeltype)
            .ThenBy(u => u.Underkategori)
            .Select(u => new SubcategoryDto
            {
                UnderkategoriId = u.UnderkategoriId,
                StempeltypeId = u.StempeltypeId,
                Underkategori = u.Underkategori,
                UnderkategoriFullTekst = u.UnderkategoriFullTekst,
                Hovedstempeltype = u.Stempeltype != null ? u.Stempeltype.Hovedstempeltype : null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, int? UnderkategoriId, string? Error)> CreateAsync(SubcategoryCreateRequest request, CancellationToken cancellationToken = default)
    {
        var code = (request.Underkategori ?? "").Trim();
        if (code.Length == 0 || code.Length > 10)
            return (false, null, "Underkategori must be 1–10 characters.");
        var exists = await _db.UnderkategoriStempeltyper
            .AnyAsync(u => u.StempeltypeId == request.StempeltypeId && u.Underkategori == code, cancellationToken);
        if (exists)
            return (false, null, "Subcategory with this code already exists for this stamp type.");
        var entity = new UnderkategoriStempeltype
        {
            StempeltypeId = request.StempeltypeId,
            Underkategori = code,
            UnderkategoriFullTekst = request.UnderkategoriFullTekst?.Trim()
        };
        _db.UnderkategoriStempeltyper.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, entity.UnderkategoriId, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int underkategoriId, SubcategoryUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.UnderkategoriStempeltyper.FindAsync([underkategoriId], cancellationToken);
        if (entity == null)
            return (false, "Subcategory not found.");
        if (request.Underkategori != null)
        {
            var code = request.Underkategori.Trim();
            if (code.Length > 10)
                return (false, "Underkategori must be at most 10 characters.");
            if (code.Length > 0)
            {
                var exists = await _db.UnderkategoriStempeltyper
                    .AnyAsync(u => u.StempeltypeId == entity.StempeltypeId && u.Underkategori == code && u.UnderkategoriId != underkategoriId, cancellationToken);
                if (exists)
                    return (false, "Subcategory with this code already exists for this stamp type.");
                entity.Underkategori = code;
            }
        }
        if (request.UnderkategoriFullTekst != null)
            entity.UnderkategoriFullTekst = request.UnderkategoriFullTekst.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int underkategoriId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.UnderkategoriStempeltyper.FindAsync([underkategoriId], cancellationToken);
        if (entity == null)
            return (false, "Subcategory not found.");
        _db.UnderkategoriStempeltyper.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}
