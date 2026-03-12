namespace PlaceNamesBlazor.Contracts.Record;

public class UpdateRecordRequest
{
    public string? Poststed { get; set; }
    public string? Kommune { get; set; }
    public int? FylkeId { get; set; }
    public int? StempeltypeId { get; set; }
    public int? UnderkategoriId { get; set; }
    public string? StempeltekstOppe { get; set; }
    public string? StempeltekstNede { get; set; }
    public string? StempeltekstMidt { get; set; }
    public string? Stempelgravoer { get; set; }
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
    public string? Kommentar { get; set; }
    /// <summary>If set, replaces the primary image.</summary>
    public string? BildePath { get; set; }
}
