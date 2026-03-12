using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Contracts.Reporting;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Data.Entities;

namespace PlaceNamesBlazor.Services.Reporting;

public class ReportingService : IReportingService
{
    private readonly PlaceNamesDbContext _db;

    public ReportingService(PlaceNamesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UsagePeriodOptionDto>> GetUsagePeriodsForStampAsync(int stempelId, CancellationToken cancellationToken = default)
    {
        var list = await _db.Bruksperioder
            .AsNoTracking()
            .Where(x => x.StempelId == stempelId)
            .OrderBy(x => x.BruksperiodeId)
            .Select(x => new UsagePeriodOptionDto
            {
                BruksperiodeId = x.BruksperiodeId,
                DisplayText = (x.DatoFoersteKjenteBruksdato ?? "N/A") + " - " + (x.DatoSisteKjenteBruksdato ?? "N/A")
            })
            .ToListAsync(cancellationToken);
        return list;
    }

    public async Task<int?> SubmitReportAsync(int rapportoerId, ReportSubmitRequest request, CancellationToken cancellationToken = default)
    {
        var reporter = await _db.Rapportoer.FirstOrDefaultAsync(r => r.RapportoerId == rapportoerId, cancellationToken);
        if (reporter == null) return null;
        if ((reporter.Status ?? "pending") != "approved")
            return null;
        if (request.ImagePaths == null || request.ImagePaths.Count == 0)
            return null;

        var report = new Rapporteringshistorikk
        {
            StempelId = request.StempelId,
            BruksperiodeId = request.BruksperiodeId,
            RapportoerId = rapportoerId,
            Rapporteringsdato = request.Rapporteringsdato,
            RapporteringFoersteSisteDato = request.RapporteringFoersteSisteDato ?? "F",
            DatoForRapportertAvtrykk = request.DatoForRapportertAvtrykk ?? "",
            CreatedAt = DateTime.UtcNow
        };
        _db.Rapporteringshistorikk.Add(report);
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var path in request.ImagePaths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            _db.RapporteringshistorikkBilder.Add(new RapporteringshistorikkBilde
            {
                RapporteringshistorikkId = report.RapporteringshistorikkId,
                BildePath = path.Trim(),
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
        return report.RapporteringshistorikkId;
    }

    public async Task<IReadOnlyList<ReportDto>> GetReportsAsync(int? rapportoerId, int? stempelId, string? status, int? limit, int offset = 0, CancellationToken cancellationToken = default)
    {
        var q = _db.Rapporteringshistorikk
            .AsNoTracking()
            .Include(x => x.Rapportoer)
            .Include(x => x.Stempel)
            .AsQueryable();
        if (rapportoerId.HasValue)
            q = q.Where(x => x.RapportoerId == rapportoerId.Value);
        if (stempelId.HasValue)
            q = q.Where(x => x.StempelId == stempelId.Value);
        if (status != null)
        {
            if (status == "pending")
                q = q.Where(x => x.GodkjentForkastet == null);
            else
                q = q.Where(x => x.GodkjentForkastet == status);
        }
        q = q.OrderByDescending(x => x.CreatedAt);
        if (offset > 0) q = q.Skip(offset);
        if (limit.HasValue) q = q.Take(limit.Value);
        var list = await q.ToListAsync(cancellationToken);
        var result = new List<ReportDto>();
        foreach (var r in list)
        {
            var images = await _db.RapporteringshistorikkBilder.AsNoTracking().Where(i => i.RapporteringshistorikkId == r.RapporteringshistorikkId).ToListAsync(cancellationToken);
            var submitterUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.RapportoerId == r.RapportoerId, cancellationToken);
            result.Add(ToDto(r, images, submitterUser?.Role));
        }
        return result;
    }

    public async Task<int> GetReportsCountAsync(int? rapportoerId, int? stempelId, string? status, CancellationToken cancellationToken = default)
    {
        var q = _db.Rapporteringshistorikk.AsNoTracking().AsQueryable();
        if (rapportoerId.HasValue)
            q = q.Where(x => x.RapportoerId == rapportoerId.Value);
        if (stempelId.HasValue)
            q = q.Where(x => x.StempelId == stempelId.Value);
        if (status != null)
        {
            if (status == "pending")
                q = q.Where(x => x.GodkjentForkastet == null);
            else
                q = q.Where(x => x.GodkjentForkastet == status);
        }
        return await q.CountAsync(cancellationToken);
    }

    public async Task<int> GetPendingReportCountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Rapporteringshistorikk
            .AsNoTracking()
            .CountAsync(x => x.GodkjentForkastet == null, cancellationToken);
    }

    public async Task<ReportDto?> GetReportByIdAsync(int rapporteringshistorikkId, CancellationToken cancellationToken = default)
    {
        var r = await _db.Rapporteringshistorikk
            .AsNoTracking()
            .Include(x => x.Rapportoer)
            .Include(x => x.Stempel)
            .FirstOrDefaultAsync(x => x.RapporteringshistorikkId == rapporteringshistorikkId, cancellationToken);
        if (r == null) return null;
        var images = await _db.RapporteringshistorikkBilder.AsNoTracking().Where(i => i.RapporteringshistorikkId == r.RapporteringshistorikkId).ToListAsync(cancellationToken);
        var submitterUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.RapportoerId == r.RapportoerId, cancellationToken);
        return ToDto(r, images, submitterUser?.Role);
    }

    public async Task<bool> ApproveReportAsync(int rapporteringshistorikkId, string initialerBeslutter, string? kommentarer, int actorUserId, CancellationToken cancellationToken = default)
    {
        var report = await _db.Rapporteringshistorikk.Include(x => x.Bilder).FirstOrDefaultAsync(x => x.RapporteringshistorikkId == rapporteringshistorikkId, cancellationToken);
        if (report == null) return false;
        var submitterUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.RapportoerId == report.RapportoerId, cancellationToken);
        var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == actorUserId, cancellationToken);
        if (submitterUser?.Role?.ToLowerInvariant() == "superuser" && actor?.Role?.ToLowerInvariant() != "admin")
            return false;
        report.GodkjentForkastet = "G";
        report.BesluttetDato = DateOnly.FromDateTime(DateTime.UtcNow);
        report.InitialerBeslutter = initialerBeslutter;
        report.UpdatedAt = DateTime.UtcNow;

        var bp = await _db.Bruksperioder.FirstOrDefaultAsync(x => x.BruksperiodeId == report.BruksperiodeId, cancellationToken);
        if (bp != null)
        {
            if (report.RapporteringFoersteSisteDato == "F")
            {
                bp.DatoFoersteKjenteBruksdato = report.DatoForRapportertAvtrykk;
                bp.RapportoerIdFoersteBruksdato = report.RapportoerId;
            }
            else
            {
                bp.DatoSisteKjenteBruksdato = report.DatoForRapportertAvtrykk;
                bp.RapportoerIdSisteBruksdato = report.RapportoerId;
            }
            if (!string.IsNullOrWhiteSpace(kommentarer))
                bp.Kommentarer = kommentarer.Length > 100 ? kommentarer[..100] : kommentarer;
            bp.UpdatedAt = DateTime.UtcNow;
        }

        var existingBilder = await _db.BruksperioderBilder.Where(x => x.BruksperiodeId == report.BruksperiodeId).ToListAsync(cancellationToken);
        var usedNumbers = existingBilder.Select(x => x.BildeNummer).ToHashSet();
        int nummer = 1;
        foreach (var img in report.Bilder.OrderBy(x => x.BildeId).Take(2))
        {
            while (usedNumbers.Contains(nummer) && nummer <= 2) nummer++;
            if (nummer > 2) break;
            _db.BruksperioderBilder.Add(new BruksperiodeBilde
            {
                BruksperiodeId = report.BruksperiodeId,
                BildePath = img.BildePath,
                BildeFilnavn = img.BildeFilnavn,
                BildeNummer = nummer,
                Beskrivelse = img.Beskrivelse,
                CreatedAt = DateTime.UtcNow
            });
            usedNumbers.Add(nummer);
            nummer++;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RejectReportAsync(int rapporteringshistorikkId, string initialerBeslutter, int actorUserId, CancellationToken cancellationToken = default)
    {
        var report = await _db.Rapporteringshistorikk.FirstOrDefaultAsync(x => x.RapporteringshistorikkId == rapporteringshistorikkId, cancellationToken);
        if (report == null) return false;
        var submitterUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.RapportoerId == report.RapportoerId, cancellationToken);
        var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == actorUserId, cancellationToken);
        if (submitterUser?.Role?.ToLowerInvariant() == "superuser" && actor?.Role?.ToLowerInvariant() != "admin")
            return false;
        report.GodkjentForkastet = "F";
        report.BesluttetDato = DateOnly.FromDateTime(DateTime.UtcNow);
        report.InitialerBeslutter = initialerBeslutter;
        report.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ReportDto ToDto(Rapporteringshistorikk r, List<RapporteringshistorikkBilde> images, string? submitterRole)
    {
        return new ReportDto
        {
            RapporteringshistorikkId = r.RapporteringshistorikkId,
            StempelId = r.StempelId,
            BruksperiodeId = r.BruksperiodeId,
            RapportoerId = r.RapportoerId,
            Rapporteringsdato = r.Rapporteringsdato,
            RapporteringFoersteSisteDato = r.RapporteringFoersteSisteDato ?? "F",
            DatoForRapportertAvtrykk = r.DatoForRapportertAvtrykk ?? "",
            GodkjentForkastet = r.GodkjentForkastet,
            BesluttetDato = r.BesluttetDato,
            InitialerBeslutter = r.InitialerBeslutter,
            CreatedAt = r.CreatedAt,
            RapportoerInitialer = r.Rapportoer?.Initialer,
            RapportoerNavn = r.Rapportoer?.FornavnEtternavn,
            StempeltekstOppe = r.Stempel?.StempeltekstOppe,
            SubmitterRole = submitterRole,
            Images = images.Select(i => new ReportImageDto { BildeId = i.BildeId, BildePath = i.BildePath, BildeFilnavn = i.BildeFilnavn }).ToList()
        };
    }
}
