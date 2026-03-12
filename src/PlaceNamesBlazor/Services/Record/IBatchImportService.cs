using PlaceNamesBlazor.Contracts.Record;

namespace PlaceNamesBlazor.Services.Record;

public interface IBatchImportService
{
    /// <summary>Process Excel stream (sheet "Fields"). If uploadedImages is provided, Bilde column is matched by filename and those bytes are uploaded to storage.</summary>
    Task<BatchImportResultDto> ProcessExcelAsync(Stream excelStream, IReadOnlyList<(string FileName, byte[] Content)>? uploadedImages = null, CancellationToken cancellationToken = default);
}
