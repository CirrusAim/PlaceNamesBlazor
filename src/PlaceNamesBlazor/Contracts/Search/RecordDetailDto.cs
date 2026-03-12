namespace PlaceNamesBlazor.Contracts.Search;

public class RecordDetailDto
{
    public int StempelId { get; set; }
    public string PoststedNavn { get; set; } = string.Empty;
    public string StempeltekstOppe { get; set; } = string.Empty;
    public string? StempeltekstNede { get; set; }
    public string? StempeltekstMidt { get; set; }
    public string? Stempelgravoer { get; set; }
    public int? UnderkategoriId { get; set; }
    public string? KommuneNavn { get; set; }
    public string? FylkeNavn { get; set; }
    public int? StempeltypeId { get; set; }
    public string? StempeltypeFullTekst { get; set; }
    public string? StempeltypeCode { get; set; }
    public string? FirstKnownDate { get; set; }
    public string? LastKnownDate { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<UsagePeriodSummaryDto> UsagePeriods { get; set; } = new();
    public string? Kommentar { get; set; }
    public string? DatoFraGravoer { get; set; }
    public string? DatoFraIntendanturTilOverordnetPostkontor { get; set; }
    public string? DatoFraOverordnetPostkontor { get; set; }
    public string? DatoForInnleveringTilOverordnetPostkontor { get; set; }
    public string? DatoInnlevertIntendantur { get; set; }
    public string? Tapsmelding { get; set; }
    public decimal? Stempeldiameter { get; set; }
    public decimal? Bokstavhoeyde { get; set; }
    public string? AndreMaal { get; set; }
    public string? Stempelfarge { get; set; }
    public string? Reparasjoner { get; set; }
    public string? DatoAvtrykkIPm { get; set; }
}

public class UsagePeriodSummaryDto
{
    public int BruksperiodeId { get; set; }
    public string? DatoFoersteKjenteBruksdato { get; set; }
    public string? DatoSisteKjenteBruksdato { get; set; }
    public string? Kommentarer { get; set; }
}
