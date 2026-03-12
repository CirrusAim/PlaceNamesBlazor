namespace PlaceNamesBlazor.Contracts.Record;

public class BatchImportResultDto
{
    public int Total { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];
}
