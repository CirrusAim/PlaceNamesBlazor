namespace PlaceNamesBlazor.Contracts.Reporting;

public class ReportDto
{
    public int RapporteringshistorikkId { get; set; }
    public int StempelId { get; set; }
    public int BruksperiodeId { get; set; }
    public int RapportoerId { get; set; }
    public DateOnly Rapporteringsdato { get; set; }
    public string RapporteringFoersteSisteDato { get; set; } = "F";
    public string DatoForRapportertAvtrykk { get; set; } = "";
    public string? GodkjentForkastet { get; set; }
    public DateOnly? BesluttetDato { get; set; }
    public string? InitialerBeslutter { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? RapportoerInitialer { get; set; }
    public string? RapportoerNavn { get; set; }
    public string? StempeltekstOppe { get; set; }
    public string? SubmitterRole { get; set; }
    public IReadOnlyList<ReportImageDto> Images { get; set; } = [];
}

public class ReportImageDto
{
    public int BildeId { get; set; }
    public string BildePath { get; set; } = "";
    public string? BildeFilnavn { get; set; }
}
