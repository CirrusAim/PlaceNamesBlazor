namespace PlaceNamesBlazor.Contracts.Search;

public class SearchRequest
{
    public string? Poststed { get; set; }
    public string? Stempeltekst { get; set; }
    public string? Kommune { get; set; }
    public int? FylkeId { get; set; }
    public int? StempeltypeId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
