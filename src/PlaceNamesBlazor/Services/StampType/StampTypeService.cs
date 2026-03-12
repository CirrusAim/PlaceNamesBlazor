using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Data.Entities;

namespace PlaceNamesBlazor.Services.StampType;

public class StampTypeService : IStampTypeService
{
    private readonly PlaceNamesDbContext _db;

    public StampTypeService(PlaceNamesDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, int? StempeltypeId, string? Error)> CreateAsync(string hovedstempeltype, string stempeltypeFullTekst, CancellationToken cancellationToken = default)
    {
        var code = (hovedstempeltype ?? "").Trim();
        if (code.Length == 0 || code.Length > 10)
            return (false, null, "Hovedstempeltype must be 1–10 characters.");
        var full = (stempeltypeFullTekst ?? "").Trim();
        if (full.Length == 0 || full.Length > 100)
            return (false, null, "Stempeltype full text must be 1–100 characters.");
        var exists = await _db.Stempeltyper.AnyAsync(s => s.Hovedstempeltype == code, cancellationToken);
        if (exists)
            return (false, null, "Stamp type with this code already exists.");
        var entity = new Stempeltype
        {
            Hovedstempeltype = code,
            StempeltypeFullTekst = full,
            MaanedsangivelseType = "A",
            Stempelutfoerelse = "S"
        };
        _db.Stempeltyper.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, entity.StempeltypeId, null);
    }
}
