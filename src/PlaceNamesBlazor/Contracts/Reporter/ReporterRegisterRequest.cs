namespace PlaceNamesBlazor.Contracts.Reporter;

public class ReporterRegisterRequest
{
    public string Initialer { get; set; } = "";
    public string FornavnEtternavn { get; set; } = "";
    public string Epost { get; set; } = "";
    public string? Telefon { get; set; }
    public string? Medlemsklubb { get; set; }
}
