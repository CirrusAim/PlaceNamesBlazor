namespace PlaceNamesBlazor.Contracts.Subcategory;

public class SubcategoryDto
{
    public int UnderkategoriId { get; set; }
    public int StempeltypeId { get; set; }
    public string Underkategori { get; set; } = string.Empty;
    public string? UnderkategoriFullTekst { get; set; }
    public string? Hovedstempeltype { get; set; }
}

public class SubcategoryCreateRequest
{
    public int StempeltypeId { get; set; }
    public string Underkategori { get; set; } = string.Empty;
    public string? UnderkategoriFullTekst { get; set; }
}

public class SubcategoryUpdateRequest
{
    public string? Underkategori { get; set; }
    public string? UnderkategoriFullTekst { get; set; }
}
