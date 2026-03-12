namespace PlaceNamesBlazor.Contracts.Reporting;

public class ReportSubmitRequest
{
    public int StempelId { get; set; }
    public int BruksperiodeId { get; set; }
    public DateOnly Rapporteringsdato { get; set; }
    public string RapporteringFoersteSisteDato { get; set; } = "F";
    public string DatoForRapportertAvtrykk { get; set; } = "";
    public IReadOnlyList<string> ImagePaths { get; set; } = [];
}
