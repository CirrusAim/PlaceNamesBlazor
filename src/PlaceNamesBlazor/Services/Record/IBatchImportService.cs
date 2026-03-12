using PlaceNamesBlazor.Contracts.Record;

namespace PlaceNamesBlazor.Services.Record;

public interface IBatchImportService
{
    /// <summary>Process Excel stream (sheet "Fields"). Returns imported count, skipped, and errors.</summary>
    Task<BatchImportResultDto> ProcessExcelAsync(Stream excelStream, CancellationToken cancellationToken = default);
}
