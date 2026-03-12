namespace PlaceNamesBlazor.Contracts.Search;

public class SearchResultDto
{
    public int StempelId { get; set; }
    public string PoststedNavn { get; set; } = string.Empty;
    public string StempeltekstOppe { get; set; } = string.Empty;
    public string? KommuneNavn { get; set; }
    public string? FylkeNavn { get; set; }
    /// <summary>Abbreviated stamp type (Hovedstempeltype/code), as shown in Flask tables.</summary>
    public string? StempeltypeCode { get; set; }
    public string? StempeltypeFullTekst { get; set; }
    public string? FirstKnownDate { get; set; }
    public string? LastKnownDate { get; set; }
    public string? ThumbnailUrl { get; set; }
}
