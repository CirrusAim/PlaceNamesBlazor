using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Contracts.UsagePeriod;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Data.Entities;

namespace PlaceNamesBlazor.Services.UsagePeriod;

public class UsagePeriodService : IUsagePeriodService
{
    private readonly PlaceNamesDbContext _db;

    public UsagePeriodService(PlaceNamesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UsagePeriodDto>> GetForStampAsync(int stempelId, CancellationToken cancellationToken = default)
    {
        return await _db.Bruksperioder
            .AsNoTracking()
            .Where(b => b.StempelId == stempelId)
            .OrderBy(b => b.DatoFoersteKjenteBruksdato)
            .Select(b => new UsagePeriodDto
            {
                BruksperiodeId = b.BruksperiodeId,
                StempelId = b.StempelId,
                DatoFoersteKjenteBruksdato = b.DatoFoersteKjenteBruksdato,
                DatoSisteKjenteBruksdato = b.DatoSisteKjenteBruksdato,
                Kommentarer = b.Kommentarer
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, int? BruksperiodeId, string? Error)> CreateAsync(int stempelId, UsagePeriodCreateRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Bruksperiode
        {
            StempelId = stempelId,
            DatoFoersteKjenteBruksdato = request.DatoFoersteKjenteBruksdato?.Trim(),
            DatoSisteKjenteBruksdato = request.DatoSisteKjenteBruksdato?.Trim(),
            Kommentarer = request.Kommentarer?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Bruksperioder.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, entity.BruksperiodeId, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int bruksperiodeId, UsagePeriodUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Bruksperioder.FindAsync([bruksperiodeId], cancellationToken);
        if (entity == null) return (false, "Usage period not found.");
        if (request.DatoFoersteKjenteBruksdato != null) entity.DatoFoersteKjenteBruksdato = request.DatoFoersteKjenteBruksdato.Trim();
        if (request.DatoSisteKjenteBruksdato != null) entity.DatoSisteKjenteBruksdato = request.DatoSisteKjenteBruksdato.Trim();
        if (request.Kommentarer != null) entity.Kommentarer = request.Kommentarer.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int bruksperiodeId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Bruksperioder.FindAsync([bruksperiodeId], cancellationToken);
        if (entity == null) return (false, "Usage period not found.");
        var bilder = await _db.BruksperioderBilder.Where(b => b.BruksperiodeId == bruksperiodeId).ToListAsync(cancellationToken);
        _db.BruksperioderBilder.RemoveRange(bilder);
        _db.Bruksperioder.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}
