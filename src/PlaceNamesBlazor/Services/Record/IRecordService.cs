using PlaceNamesBlazor.Contracts.Record;

namespace PlaceNamesBlazor.Services.Record;

/// <summary>Record (stempel) CRUD. Admin/superuser only; enforce in UI or middleware.</summary>
public interface IRecordService
{
    Task<(bool Success, string? Error)> DeleteAsync(int stempelId, CancellationToken cancellationToken = default);
    Task<(int DeletedCount, IReadOnlyList<string> Errors)> BulkDeleteAsync(IEnumerable<int> stempelIds, CancellationToken cancellationToken = default);
    Task<(bool Success, int? StempelId, string? Error)> CreateAsync(CreateRecordRequest request, CancellationToken cancellationToken = default);
    /// <summary>Batch create records with batched SaveChanges for faster import (e.g. 1000+ rows). Returns (imported count, errors by row).</summary>
    Task<(int Imported, IReadOnlyList<string> Errors)> CreateBatchAsync(IReadOnlyList<(int RowIndex, CreateRecordRequest Request)> items, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(int stempelId, UpdateRecordRequest request, CancellationToken cancellationToken = default);
}
