using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Contracts.Dropdowns;
using PlaceNamesBlazor.Data;

namespace PlaceNamesBlazor.Services;

public class DropdownService : IDropdownService
{
    private readonly PlaceNamesDbContext _db;

    public DropdownService(PlaceNamesDbContext db)
    {
        _db = db;
    }

    public async Task<DropdownsDto> GetDropdownsAsync(CancellationToken cancellationToken = default)
    {
        var fylker = await _db.Fylker
            .OrderBy(f => f.FylkeNavn)
            .Select(f => new FylkeItemDto { FylkeId = f.FylkeId, FylkeNavn = f.FylkeNavn })
            .ToListAsync(cancellationToken);
        var stempeltyper = await _db.Stempeltyper
            .OrderBy(s => s.Hovedstempeltype)
            .Select(s => new StempeltypeItemDto
            {
                StempeltypeId = s.StempeltypeId,
                Hovedstempeltype = s.Hovedstempeltype,
                StempeltypeFullTekst = s.StempeltypeFullTekst
            })
            .ToListAsync(cancellationToken);
        var engravers = GetEngraverOptions();
        return new DropdownsDto { Fylker = fylker, Stempeltyper = stempeltyper, Engravers = engravers };
    }

    private static IReadOnlyList<EngraverItemDto> GetEngraverOptions()
    {
        return
        [
            new EngraverItemDto { Code = "S", Name = "Schwarzenhorn" },
            new EngraverItemDto { Code = "L", Name = "Carl Lundgren" },
            new EngraverItemDto { Code = "Sy", Name = "Fabrikk \"Sylvan\", Kristiania" },
            new EngraverItemDto { Code = "G", Name = "Garmann" },
            new EngraverItemDto { Code = "T", Name = "Ivar Throndsen" },
            new EngraverItemDto { Code = "C", Name = "Christiania Chablon- & Stempelfabrik" },
            new EngraverItemDto { Code = "M", Name = "Mignon Chablon- & Stempelfabrik" },
            new EngraverItemDto { Code = "R", Name = "Hellik Rui og Halfdan Rui" },
            new EngraverItemDto { Code = "K", Name = "Krag Maskin Fabrik A/S" },
            new EngraverItemDto { Code = "Annen", Name = "Other" },
            new EngraverItemDto { Code = "", Name = "Unknown/Not specified" }
        ];
    }
}
