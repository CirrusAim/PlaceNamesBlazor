namespace PlaceNamesBlazor.Contracts.Record;

public class CreateRecordRequest
{
    public string Poststed { get; set; } = string.Empty;
    public string Kommune { get; set; } = string.Empty;
    public int FylkeId { get; set; }
    public int StempeltypeId { get; set; }
    public int? UnderkategoriId { get; set; }
    public string StempeltekstOppe { get; set; } = string.Empty;
    public string? StempeltekstNede { get; set; }
    public string? StempeltekstMidt { get; set; }
    /// <summary>Engraver code (stempelgravoer) - S, L, Sy, G, T, C, M, R, K, Annen, or empty.</summary>
    public string? Stempelgravoer { get; set; }
    /// <summary>Date from engraver (yyyy-MM-dd).</summary>
    public string? DatoFraGravoer { get; set; }
    /// <summary>Date from intendantur to overordnet postkontor (yyyy-MM-dd).</summary>
    public string? DatoFraIntendanturTilOverordnetPostkontor { get; set; }
    /// <summary>Date from overordnet postkontor (yyyy-MM-dd).</summary>
    public string? DatoFraOverordnetPostkontor { get; set; }
    /// <summary>Date for innlevering to overordnet postkontor (yyyy-MM-dd).</summary>
    public string? DatoForInnleveringTilOverordnetPostkontor { get; set; }
    /// <summary>Date innlevert intendantur (yyyy-MM-dd).</summary>
    public string? DatoInnlevertIntendantur { get; set; }
    public string? Tapsmelding { get; set; }
    public decimal? Stempeldiameter { get; set; }
    public decimal? Bokstavhoeyde { get; set; }
    public string? AndreMaal { get; set; }
    public string? Stempelfarge { get; set; }
    public string? Reparasjoner { get; set; }
    public string? DatoAvtrykkIPm { get; set; }
    public string? Kommentar { get; set; }
    /// <summary>Optional primary image path (e.g. from upload).</summary>
    public string? BildePath { get; set; }
    /// <summary>First known date for the default usage period (string e.g. YYYY-MM-DD).</summary>
    public string? ForsteKjente { get; set; }
    /// <summary>Last known date for the default usage period.</summary>
    public string? SisteKjente { get; set; }
    /// <summary>Usage period comment.</summary>
    public string? BruksperiodeKommentarer { get; set; }
}
