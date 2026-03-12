namespace PlaceNamesBlazor.Contracts.UsagePeriod;

public class UsagePeriodDto
{
    public int BruksperiodeId { get; set; }
    public int StempelId { get; set; }
    public string? DatoFoersteKjenteBruksdato { get; set; }
    public string? DatoSisteKjenteBruksdato { get; set; }
    public string? Kommentarer { get; set; }
}

public class UsagePeriodCreateRequest
{
    public string? DatoFoersteKjenteBruksdato { get; set; }
    public string? DatoSisteKjenteBruksdato { get; set; }
    public string? Kommentarer { get; set; }
}

public class UsagePeriodUpdateRequest
{
    public string? DatoFoersteKjenteBruksdato { get; set; }
    public string? DatoSisteKjenteBruksdato { get; set; }
    public string? Kommentarer { get; set; }
}
