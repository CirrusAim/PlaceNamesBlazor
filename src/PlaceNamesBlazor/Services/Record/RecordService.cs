using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Contracts.Record;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Data.Entities;

namespace PlaceNamesBlazor.Services.Record;

public class RecordService : IRecordService
{
    private const int BatchSaveSize = 100;

    private readonly PlaceNamesDbContext _db;
    private readonly IDbContextFactory<PlaceNamesDbContext> _factory;

    public RecordService(PlaceNamesDbContext db, IDbContextFactory<PlaceNamesDbContext> factory)
    {
        _db = db;
        _factory = factory;
    }

    public async Task<(bool Success, int? StempelId, string? Error)> CreateAsync(CreateRecordRequest request, CancellationToken cancellationToken = default)
    {
        var poststed = (request.Poststed ?? "").Trim();
        var kommune = (request.Kommune ?? "").Trim();
        if (string.IsNullOrEmpty(poststed) || string.IsNullOrEmpty(kommune))
            return (false, null, "Poststed and Kommune are required.");
        if (string.IsNullOrEmpty(request.StempeltekstOppe))
            request.StempeltekstOppe = poststed;

        try
        {
            var kommuneObj = await _db.Kommuner
                .FirstOrDefaultAsync(k => k.Kommunenavn == kommune && k.FylkeId == request.FylkeId, cancellationToken);
            if (kommuneObj == null)
            {
                kommuneObj = new Kommune { Kommunenavn = kommune, FylkeId = request.FylkeId };
                _db.Kommuner.Add(kommuneObj);
                await _db.SaveChangesAsync(cancellationToken);
            }

            var poststedObj = await _db.Poststeder
                .FirstOrDefaultAsync(p => p.PoststedNavn == poststed && p.KommuneId == kommuneObj.KommuneId, cancellationToken);
            if (poststedObj == null)
            {
                poststedObj = new Poststed { PoststedNavn = poststed, KommuneId = kommuneObj.KommuneId };
                _db.Poststeder.Add(poststedObj);
                await _db.SaveChangesAsync(cancellationToken);
            }

            var stempel = new Stempel
            {
                PoststedId = poststedObj.PoststedId,
                StempeltypeId = request.StempeltypeId,
                UnderkategoriId = request.UnderkategoriId,
                StempeltekstOppe = request.StempeltekstOppe.Trim(),
                StempeltekstNede = request.StempeltekstNede?.Trim(),
                StempeltekstMidt = request.StempeltekstMidt?.Trim(),
                Stempelgravoer = string.IsNullOrWhiteSpace(request.Stempelgravoer) ? null : request.Stempelgravoer.Trim(),
                DatoFraGravoer = ParseDateOnly(request.DatoFraGravoer),
                DatoFraIntendanturTilOverordnetPostkontor = ParseDateOnly(request.DatoFraIntendanturTilOverordnetPostkontor),
                DatoFraOverordnetPostkontor = ParseDateOnly(request.DatoFraOverordnetPostkontor),
                DatoForInnleveringTilOverordnetPostkontor = ParseDateOnly(request.DatoForInnleveringTilOverordnetPostkontor),
                DatoInnlevertIntendantur = ParseDateOnly(request.DatoInnlevertIntendantur),
                Tapsmelding = request.Tapsmelding?.Trim(),
                Stempeldiameter = request.Stempeldiameter,
                Bokstavhoeyde = request.Bokstavhoeyde,
                AndreMaal = request.AndreMaal?.Trim(),
                Stempelfarge = request.Stempelfarge?.Trim(),
                Reparasjoner = request.Reparasjoner?.Trim(),
                DatoAvtrykkIPm = request.DatoAvtrykkIPm?.Trim(),
                Kommentar = request.Kommentar?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Stempler.Add(stempel);
            await _db.SaveChangesAsync(cancellationToken);

            var bruksperiode = new Bruksperiode
            {
                StempelId = stempel.StempelId,
                DatoFoersteKjenteBruksdato = request.ForsteKjente?.Trim(),
                DatoSisteKjenteBruksdato = request.SisteKjente?.Trim(),
                Kommentarer = request.BruksperiodeKommentarer?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Bruksperioder.Add(bruksperiode);
            await _db.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.BildePath))
            {
                var path = request.BildePath.Trim();
                var filnavn = Path.GetFileName(path);
                _db.Stempelbilder.Add(new Stempelbilde
                {
                    StempelId = stempel.StempelId,
                    BildePath = path,
                    BildeFilnavn = filnavn,
                    ErPrimær = true,
                    CreatedAt = DateTime.UtcNow
                });
                _db.BruksperioderBilder.Add(new BruksperiodeBilde
                {
                    BruksperiodeId = bruksperiode.BruksperiodeId,
                    BildePath = path,
                    BildeFilnavn = filnavn,
                    BildeNummer = 1,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(cancellationToken);
            }

            return (true, stempel.StempelId, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(int Imported, IReadOnlyList<string> Errors)> CreateBatchAsync(IReadOnlyList<(int RowIndex, CreateRecordRequest Request)> items, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        if (items.Count == 0)
            return (0, errors);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        try
        {
            var uniqueKommuneKeys = items
                .Select(x => (x.Request.FylkeId, Kommune: (x.Request.Kommune ?? "").Trim()))
                .Where(x => !string.IsNullOrEmpty(x.Kommune))
                .Distinct()
                .ToList();
            var kommuneKeySet = uniqueKommuneKeys.ToHashSet();
            var fylkeIds = uniqueKommuneKeys.Select(u => u.FylkeId).Distinct().ToList();
            var existingKommuner = (await db.Kommuner
                .Where(k => fylkeIds.Contains(k.FylkeId))
                .ToListAsync(cancellationToken))
                .Where(k => kommuneKeySet.Contains((k.FylkeId, k.Kommunenavn)))
                .ToList();
            var kommuneByKey = existingKommuner.ToDictionary(k => (k.FylkeId, k.Kommunenavn));
            foreach (var (fylkeId, kommuneNavn) in uniqueKommuneKeys)
            {
                if (kommuneByKey.ContainsKey((fylkeId, kommuneNavn))) continue;
                var k = new Kommune { Kommunenavn = kommuneNavn, FylkeId = fylkeId };
                db.Kommuner.Add(k);
                kommuneByKey[(fylkeId, kommuneNavn)] = k;
            }
            await db.SaveChangesAsync(cancellationToken);
            foreach (var k in db.Kommuner.Local.Where(k => k.KommuneId != 0))
                kommuneByKey[(k.FylkeId, k.Kommunenavn)] = k;

            var uniquePoststedKeys = items
                .Select(x =>
                {
                    var kom = (x.Request.Kommune ?? "").Trim();
                    var post = (x.Request.Poststed ?? "").Trim();
                    if (string.IsNullOrEmpty(post) || !kommuneByKey.TryGetValue((x.Request.FylkeId, kom), out var ko))
                        return ((int?)null, (string?)null);
                    return ((int?)ko.KommuneId, (string?)post);
                })
                .Where(x => x.Item1.HasValue && !string.IsNullOrEmpty(x.Item2))
                .Distinct()
                .ToList();
            var kommuneIds = uniquePoststedKeys.Select(x => x.Item1!.Value).Distinct().ToList();
            var poststedKeySet = uniquePoststedKeys
                .Where(u => u.Item1.HasValue && !string.IsNullOrEmpty(u.Item2))
                .Select(u => (KommuneId: u.Item1!.Value, PoststedNavn: u.Item2!.Trim()))
                .ToHashSet();
            var existingPoststeder = await db.Poststeder
                .Where(p => p.KommuneId.HasValue && kommuneIds.Contains(p.KommuneId!.Value))
                .ToListAsync(cancellationToken);
            existingPoststeder = existingPoststeder.Where(p => poststedKeySet.Contains((p.KommuneId!.Value, p.PoststedNavn))).ToList();
            var poststedByKey = existingPoststeder.ToDictionary(p => (p.KommuneId!.Value, p.PoststedNavn));
            foreach (var (kommuneId, poststedNavn) in uniquePoststedKeys)
            {
                if (!kommuneId.HasValue || string.IsNullOrEmpty(poststedNavn)) continue;
                var key = (kommuneId.Value, poststedNavn.Trim());
                if (poststedByKey.ContainsKey(key)) continue;
                var p = new Poststed { PoststedNavn = key.Item2, KommuneId = key.Item1 };
                db.Poststeder.Add(p);
                poststedByKey[key] = p;
            }
            await db.SaveChangesAsync(cancellationToken);
            foreach (var p in db.Poststeder.Local.Where(p => p.PoststedId != 0 && p.KommuneId.HasValue))
                poststedByKey[(p.KommuneId!.Value, p.PoststedNavn)] = p;

            int imported = 0;
            int batchCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var (rowIndex, request) = items[i];
                var poststed = (request.Poststed ?? "").Trim();
                var kommune = (request.Kommune ?? "").Trim();
                if (string.IsNullOrEmpty(poststed) || string.IsNullOrEmpty(kommune))
                {
                    errors.Add($"Row {rowIndex}: Poststed and Kommune are required.");
                    continue;
                }
                if (!kommuneByKey.TryGetValue((request.FylkeId, kommune), out var kommuneObj))
                {
                    errors.Add($"Row {rowIndex}: Kommune not found.");
                    continue;
                }
                if (!poststedByKey.TryGetValue((kommuneObj.KommuneId, poststed), out var poststedObj))
                {
                    errors.Add($"Row {rowIndex}: Poststed not found.");
                    continue;
                }
                var stempeltekstOppe = string.IsNullOrEmpty(request.StempeltekstOppe?.Trim()) ? poststed : request.StempeltekstOppe!.Trim();

                var stempel = new Stempel
                {
                    PoststedId = poststedObj.PoststedId,
                    StempeltypeId = request.StempeltypeId,
                    UnderkategoriId = request.UnderkategoriId,
                    StempeltekstOppe = stempeltekstOppe,
                    StempeltekstNede = request.StempeltekstNede?.Trim(),
                    StempeltekstMidt = request.StempeltekstMidt?.Trim(),
                    Stempelgravoer = string.IsNullOrWhiteSpace(request.Stempelgravoer) ? null : request.Stempelgravoer.Trim(),
                    DatoFraGravoer = ParseDateOnly(request.DatoFraGravoer),
                    DatoFraIntendanturTilOverordnetPostkontor = ParseDateOnly(request.DatoFraIntendanturTilOverordnetPostkontor),
                    DatoFraOverordnetPostkontor = ParseDateOnly(request.DatoFraOverordnetPostkontor),
                    DatoForInnleveringTilOverordnetPostkontor = ParseDateOnly(request.DatoForInnleveringTilOverordnetPostkontor),
                    DatoInnlevertIntendantur = ParseDateOnly(request.DatoInnlevertIntendantur),
                    Tapsmelding = request.Tapsmelding?.Trim(),
                    Stempeldiameter = request.Stempeldiameter,
                    Bokstavhoeyde = request.Bokstavhoeyde,
                    AndreMaal = request.AndreMaal?.Trim(),
                    Stempelfarge = request.Stempelfarge?.Trim(),
                    Reparasjoner = request.Reparasjoner?.Trim(),
                    DatoAvtrykkIPm = request.DatoAvtrykkIPm?.Trim(),
                    Kommentar = request.Kommentar?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Stempler.Add(stempel);
                batchCount++;

                var bruksperiode = new Bruksperiode
                {
                    Stempel = stempel,
                    DatoFoersteKjenteBruksdato = request.ForsteKjente?.Trim(),
                    DatoSisteKjenteBruksdato = request.SisteKjente?.Trim(),
                    Kommentarer = request.BruksperiodeKommentarer?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Bruksperioder.Add(bruksperiode);

                if (!string.IsNullOrWhiteSpace(request.BildePath))
                {
                    var path = request.BildePath.Trim();
                    var filnavn = Path.GetFileName(path);
                    db.Stempelbilder.Add(new Stempelbilde
                    {
                        Stempel = stempel,
                        BildePath = path,
                        BildeFilnavn = filnavn,
                        ErPrimær = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    db.BruksperioderBilder.Add(new BruksperiodeBilde
                    {
                        Bruksperiode = bruksperiode,
                        BildePath = path,
                        BildeFilnavn = filnavn,
                        BildeNummer = 1,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (batchCount >= BatchSaveSize)
                {
                    await db.SaveChangesAsync(cancellationToken);
                    imported += batchCount;
                    batchCount = 0;
                }
            }

            if (batchCount > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
                imported += batchCount;
            }

            return (imported, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Batch failed: {ex.Message}");
            return (0, errors);
        }
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int stempelId, UpdateRecordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var stempel = await _db.Stempler
                .Include(s => s.Poststed)
                .ThenInclude(p => p!.Kommune)
                .FirstOrDefaultAsync(s => s.StempelId == stempelId, cancellationToken);
            if (stempel == null)
                return (false, "Record not found.");

            if (request.StempeltypeId.HasValue)
                stempel.StempeltypeId = request.StempeltypeId.Value;
            if (request.UnderkategoriId.HasValue)
                stempel.UnderkategoriId = request.UnderkategoriId.Value == 0 ? null : request.UnderkategoriId;
            if (request.StempeltekstOppe != null)
                stempel.StempeltekstOppe = request.StempeltekstOppe;
            if (request.StempeltekstNede != null)
                stempel.StempeltekstNede = request.StempeltekstNede;
            if (request.StempeltekstMidt != null)
                stempel.StempeltekstMidt = request.StempeltekstMidt;
            if (request.Stempelgravoer != null)
                stempel.Stempelgravoer = string.IsNullOrWhiteSpace(request.Stempelgravoer) ? null : request.Stempelgravoer.Trim();
            if (request.Kommentar != null)
                stempel.Kommentar = request.Kommentar;
            if (request.DatoFraGravoer != null) stempel.DatoFraGravoer = ParseDateOnly(request.DatoFraGravoer);
            if (request.DatoFraIntendanturTilOverordnetPostkontor != null) stempel.DatoFraIntendanturTilOverordnetPostkontor = ParseDateOnly(request.DatoFraIntendanturTilOverordnetPostkontor);
            if (request.DatoFraOverordnetPostkontor != null) stempel.DatoFraOverordnetPostkontor = ParseDateOnly(request.DatoFraOverordnetPostkontor);
            if (request.DatoForInnleveringTilOverordnetPostkontor != null) stempel.DatoForInnleveringTilOverordnetPostkontor = ParseDateOnly(request.DatoForInnleveringTilOverordnetPostkontor);
            if (request.DatoInnlevertIntendantur != null) stempel.DatoInnlevertIntendantur = ParseDateOnly(request.DatoInnlevertIntendantur);
            if (request.Tapsmelding != null) stempel.Tapsmelding = request.Tapsmelding.Trim();
            if (request.Stempeldiameter.HasValue) stempel.Stempeldiameter = request.Stempeldiameter;
            if (request.Bokstavhoeyde.HasValue) stempel.Bokstavhoeyde = request.Bokstavhoeyde;
            if (request.AndreMaal != null) stempel.AndreMaal = request.AndreMaal.Trim();
            if (request.Stempelfarge != null) stempel.Stempelfarge = request.Stempelfarge.Trim();
            if (request.Reparasjoner != null) stempel.Reparasjoner = request.Reparasjoner.Trim();
            if (request.DatoAvtrykkIPm != null) stempel.DatoAvtrykkIPm = request.DatoAvtrykkIPm.Trim();

            if (request.Poststed != null && request.Kommune != null && request.FylkeId.HasValue)
            {
                var poststed = request.Poststed.Trim();
                var kommune = request.Kommune.Trim();
                var kommuneObj = await _db.Kommuner
                    .FirstOrDefaultAsync(k => k.Kommunenavn == kommune && k.FylkeId == request.FylkeId.Value, cancellationToken);
                if (kommuneObj == null)
                {
                    kommuneObj = new Kommune { Kommunenavn = kommune, FylkeId = request.FylkeId.Value };
                    _db.Kommuner.Add(kommuneObj);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                var poststedObj = await _db.Poststeder
                    .FirstOrDefaultAsync(p => p.PoststedNavn == poststed && p.KommuneId == kommuneObj.KommuneId, cancellationToken);
                if (poststedObj == null)
                {
                    poststedObj = new Poststed { PoststedNavn = poststed, KommuneId = kommuneObj.KommuneId };
                    _db.Poststeder.Add(poststedObj);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                stempel.PoststedId = poststedObj.PoststedId;
            }

            if (request.BildePath != null)
            {
                var path = request.BildePath.Trim();
                if (string.IsNullOrEmpty(path))
                {
                    await _db.Stempelbilder.Where(b => b.StempelId == stempelId).ExecuteDeleteAsync(cancellationToken);
                }
                else
                {
                    var existing = await _db.Stempelbilder.Where(b => b.StempelId == stempelId).ToListAsync(cancellationToken);
                    _db.Stempelbilder.RemoveRange(existing);
                    _db.Stempelbilder.Add(new Stempelbilde
                    {
                        StempelId = stempelId,
                        BildePath = path,
                        BildeFilnavn = Path.GetFileName(path),
                        ErPrimær = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            stempel.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int stempelId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rhIds = await _db.Rapporteringshistorikk.Where(r => r.StempelId == stempelId).Select(r => r.RapporteringshistorikkId).ToListAsync(cancellationToken);
            foreach (var rhId in rhIds)
                await _db.RapporteringshistorikkBilder.Where(b => b.RapporteringshistorikkId == rhId).ExecuteDeleteAsync(cancellationToken);
            await _db.Rapporteringshistorikk.Where(r => r.StempelId == stempelId).ExecuteDeleteAsync(cancellationToken);

            var bpIds = await _db.Bruksperioder.Where(b => b.StempelId == stempelId).Select(b => b.BruksperiodeId).ToListAsync(cancellationToken);
            foreach (var bpId in bpIds)
                await _db.BruksperioderBilder.Where(b => b.BruksperiodeId == bpId).ExecuteDeleteAsync(cancellationToken);
            await _db.Bruksperioder.Where(b => b.StempelId == stempelId).ExecuteDeleteAsync(cancellationToken);

            await _db.Stempelbilder.Where(s => s.StempelId == stempelId).ExecuteDeleteAsync(cancellationToken);
            var deleted = await _db.Stempler.Where(s => s.StempelId == stempelId).ExecuteDeleteAsync(cancellationToken);
            return (deleted > 0, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(int DeletedCount, IReadOnlyList<string> Errors)> BulkDeleteAsync(IEnumerable<int> stempelIds, CancellationToken cancellationToken = default)
    {
        var ids = stempelIds.Distinct().ToList();
        var errors = new List<string>();
        var deleted = 0;
        foreach (var id in ids)
        {
            var (ok, err) = await DeleteAsync(id, cancellationToken);
            if (ok) deleted++;
            else if (!string.IsNullOrEmpty(err)) errors.Add($"Stempel {id}: {err}");
        }
        return (deleted, errors);
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateOnly.TryParse(value.Trim(), out var d)) return d;
        return null;
    }
}
