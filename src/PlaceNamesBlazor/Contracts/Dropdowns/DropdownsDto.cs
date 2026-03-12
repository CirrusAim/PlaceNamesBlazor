namespace PlaceNamesBlazor.Contracts.Dropdowns;

public class DropdownsDto
{
    public IReadOnlyList<FylkeItemDto> Fylker { get; set; } = [];
    public IReadOnlyList<StempeltypeItemDto> Stempeltyper { get; set; } = [];
    /// <summary>Engraver options (stempelgravoer) - code and display name.</summary>
    public IReadOnlyList<EngraverItemDto> Engravers { get; set; } = [];
}

public class EngraverItemDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class FylkeItemDto
{
    public int FylkeId { get; set; }
    public string FylkeNavn { get; set; } = string.Empty;
}

public class StempeltypeItemDto
{
    public int StempeltypeId { get; set; }
    public string Hovedstempeltype { get; set; } = string.Empty;
    public string StempeltypeFullTekst { get; set; } = string.Empty;
}
