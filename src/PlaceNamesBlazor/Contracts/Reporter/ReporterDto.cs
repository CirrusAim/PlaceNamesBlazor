namespace PlaceNamesBlazor.Contracts.Reporter;

public class ReporterDto
{
    public int RapportoerId { get; set; }
    public string Initialer { get; set; } = "";
    public string FornavnEtternavn { get; set; } = "";
    public string Epost { get; set; } = "";
    public string? Telefon { get; set; }
    public string? Medlemsklubb { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
