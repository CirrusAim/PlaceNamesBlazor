using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PlaceNamesBlazor.Contracts.Common;
using PlaceNamesBlazor.Contracts.Search;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Services.ImageStorage;

namespace PlaceNamesBlazor.Services;

public class StampSearchService : IStampSearchService
{
    private const int GuestSearchLimit = 20;
    private static readonly TimeSpan GuestCountCacheSlidingExpiration = TimeSpan.FromMinutes(20);

    private readonly PlaceNamesDbContext _db;
    private readonly IImageStorageService _imageStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;

    public StampSearchService(PlaceNamesDbContext db, IImageStorageService imageStorage, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
    {
        _db = db;
        _imageStorage = imageStorage;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }

    public async Task<SearchResponseDto> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var isGuest = httpContext?.User?.Identity?.IsAuthenticated != true;
        var guestId = httpContext?.Items["GuestSearchId"] as string
            ?? httpContext?.Request.Cookies["guest_search_id"]
            ?? httpContext?.Connection?.Id;
        string? guestKey = isGuest ? "GuestSearch_" + (guestId ?? Guid.NewGuid().ToString()) : null;

        if (isGuest && guestKey != null)
        {
            var count = _cache.GetOrCreate(guestKey, entry =>
            {
                entry.SlidingExpiration = GuestCountCacheSlidingExpiration;
                return 0;
            });
            count++;
            _cache.Set(guestKey, count, new MemoryCacheEntryOptions { SlidingExpiration = GuestCountCacheSlidingExpiration });
            if (count > GuestSearchLimit)
            {
                return new SearchResponseDto
                {
                    LimitExceeded = true,
                    LimitExceededMessage = $"Guest search limit reached ({GuestSearchLimit} searches). Please register or login to continue searching.",
                    GuestSearchUsed = count,
                    GuestSearchLimit = GuestSearchLimit
                };
            }
        }
        var q = from s in _db.Stempler
                join p in _db.Poststeder on s.PoststedId equals p.PoststedId
                join st in _db.Stempeltyper on s.StempeltypeId equals st.StempeltypeId
                from k in _db.Kommuner.Where(k => k.KommuneId == p.KommuneId).DefaultIfEmpty()
                from f in _db.Fylker.Where(f => f.FylkeId == k!.FylkeId).DefaultIfEmpty()
                from bp in _db.Bruksperioder.Where(bp => bp.StempelId == s.StempelId).DefaultIfEmpty()
                from img in _db.Stempelbilder.Where(i => i.StempelId == s.StempelId && i.ErPrimær).DefaultIfEmpty()
                select new { s, p, st, k, f, bp, img };

        if (!string.IsNullOrWhiteSpace(request.Poststed))
        {
            var term = request.Poststed.Trim().ToLower();
            q = q.Where(x => x.p.PoststedNavn.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(request.Stempeltekst))
        {
            var term = request.Stempeltekst.Trim().ToLower();
            q = q.Where(x => x.s.StempeltekstOppe.ToLower().Contains(term) ||
                             (x.s.StempeltekstNede != null && x.s.StempeltekstNede.ToLower().Contains(term)) ||
                             (x.s.StempeltekstMidt != null && x.s.StempeltekstMidt.ToLower().Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(request.Kommune))
        {
            var term = request.Kommune.Trim().ToLower();
            q = q.Where(x => x.k != null && x.k.Kommunenavn.ToLower().Contains(term));
        }
        if (request.FylkeId.HasValue)
            q = q.Where(x => x.f != null && x.f.FylkeId == request.FylkeId.Value);
        if (request.StempeltypeId.HasValue)
            q = q.Where(x => x.st.StempeltypeId == request.StempeltypeId.Value);

        // First/last known from min/max bruksperiode_id with dato_foerste_kjente_bruksdato / dato_siste_kjente_bruksdato
        var grouped = q.GroupBy(x => new { x.s.StempelId, x.s.StempeltekstOppe, x.p.PoststedNavn, KommuneNavn = x.k != null ? x.k.Kommunenavn : null, FylkeNavn = x.f != null ? x.f.FylkeNavn : null, x.st.Hovedstempeltype, x.st.StempeltypeFullTekst, BildePath = x.img != null ? x.img.BildePath : null })
            .Select(g => new
            {
                g.Key.StempelId,
                g.Key.StempeltekstOppe,
                g.Key.PoststedNavn,
                g.Key.KommuneNavn,
                g.Key.FylkeNavn,
                g.Key.Hovedstempeltype,
                g.Key.StempeltypeFullTekst,
                g.Key.BildePath,
                FirstKnown = g.Where(x => x.bp != null && x.bp.DatoFoersteKjenteBruksdato != null).OrderBy(x => x.bp!.BruksperiodeId).Select(x => x.bp!.DatoFoersteKjenteBruksdato).FirstOrDefault(),
                LastKnown = g.Where(x => x.bp != null && x.bp.DatoSisteKjenteBruksdato != null).OrderByDescending(x => x.bp!.BruksperiodeId).Select(x => x.bp!.DatoSisteKjenteBruksdato).FirstOrDefault()
            });

        var total = await grouped.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page = Math.Max(1, request.Page);
        var items = await grouped
            .OrderBy(x => x.PoststedNavn)
            .ThenBy(x => x.StempeltekstOppe)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new SearchResultDto
        {
            StempelId = x.StempelId,
            PoststedNavn = x.PoststedNavn,
            StempeltekstOppe = x.StempeltekstOppe,
            KommuneNavn = x.KommuneNavn,
            FylkeNavn = x.FylkeNavn,
            StempeltypeCode = x.Hovedstempeltype,
            StempeltypeFullTekst = x.StempeltypeFullTekst,
            FirstKnownDate = x.FirstKnown,
            LastKnownDate = x.LastKnown,
            ThumbnailUrl = string.IsNullOrEmpty(x.BildePath) ? null : _imageStorage.GetDisplayUrl(x.BildePath)
        }).ToList();

        var result = new PagedResultDto<SearchResultDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
        var response = new SearchResponseDto { Result = result };
        if (isGuest && guestKey != null)
        {
            var used = _cache.Get<int>(guestKey);
            response.GuestSearchUsed = used;
            response.GuestSearchLimit = GuestSearchLimit;
        }
        return response;
    }

    public async Task<RecordDetailDto?> GetByIdAsync(int stempelId, CancellationToken cancellationToken = default)
    {
        var s = await _db.Stempler
            .AsNoTracking()
            .Include(x => x.Poststed)
            .ThenInclude(p => p!.Kommune)
            .ThenInclude(k => k!.Fylke)
            .Include(x => x.Stempeltype)
            .Include(x => x.Bruksperioder)
            .Include(x => x.Stempelbilder.Where(i => i.ErPrimær))
            .FirstOrDefaultAsync(x => x.StempelId == stempelId, cancellationToken);
        if (s == null) return null;

        // Same logic for record detail: first/last known from first/last bruksperiode by id
        var firstBp = s.Bruksperioder.Where(b => b.DatoFoersteKjenteBruksdato != null).OrderBy(b => b.BruksperiodeId).FirstOrDefault();
        var lastBp = s.Bruksperioder.Where(b => b.DatoSisteKjenteBruksdato != null).OrderByDescending(b => b.BruksperiodeId).FirstOrDefault();
        var primaryImg = s.Stempelbilder.FirstOrDefault();
        return new RecordDetailDto
        {
            StempelId = s.StempelId,
            PoststedNavn = s.Poststed?.PoststedNavn ?? "",
            StempeltekstOppe = s.StempeltekstOppe,
            StempeltekstNede = s.StempeltekstNede,
            StempeltekstMidt = s.StempeltekstMidt,
            Stempelgravoer = s.Stempelgravoer,
            UnderkategoriId = s.UnderkategoriId,
            KommuneNavn = s.Poststed?.Kommune?.Kommunenavn,
            FylkeNavn = s.Poststed?.Kommune?.Fylke?.FylkeNavn,
            StempeltypeId = s.StempeltypeId,
            StempeltypeFullTekst = s.Stempeltype?.StempeltypeFullTekst,
            StempeltypeCode = s.Stempeltype?.Hovedstempeltype,
            FirstKnownDate = firstBp?.DatoFoersteKjenteBruksdato,
            LastKnownDate = lastBp?.DatoSisteKjenteBruksdato,
            ThumbnailUrl = primaryImg != null && !string.IsNullOrEmpty(primaryImg.BildePath) ? _imageStorage.GetDisplayUrl(primaryImg.BildePath) : null,
            UsagePeriods = s.Bruksperioder.Select(b => new UsagePeriodSummaryDto
            {
                BruksperiodeId = b.BruksperiodeId,
                DatoFoersteKjenteBruksdato = b.DatoFoersteKjenteBruksdato,
                DatoSisteKjenteBruksdato = b.DatoSisteKjenteBruksdato,
                Kommentarer = b.Kommentarer
            }).ToList(),
            Kommentar = s.Kommentar,
            DatoFraGravoer = s.DatoFraGravoer?.ToString("yyyy-MM-dd"),
            DatoFraIntendanturTilOverordnetPostkontor = s.DatoFraIntendanturTilOverordnetPostkontor?.ToString("yyyy-MM-dd"),
            DatoFraOverordnetPostkontor = s.DatoFraOverordnetPostkontor?.ToString("yyyy-MM-dd"),
            DatoForInnleveringTilOverordnetPostkontor = s.DatoForInnleveringTilOverordnetPostkontor?.ToString("yyyy-MM-dd"),
            DatoInnlevertIntendantur = s.DatoInnlevertIntendantur?.ToString("yyyy-MM-dd"),
            Tapsmelding = s.Tapsmelding,
            Stempeldiameter = s.Stempeldiameter,
            Bokstavhoeyde = s.Bokstavhoeyde,
            AndreMaal = s.AndreMaal,
            Stempelfarge = s.Stempelfarge,
            Reparasjoner = s.Reparasjoner,
            DatoAvtrykkIPm = s.DatoAvtrykkIPm
        };
    }
}
