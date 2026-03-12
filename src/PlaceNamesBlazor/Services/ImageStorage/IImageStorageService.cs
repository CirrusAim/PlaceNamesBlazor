namespace PlaceNamesBlazor.Services.ImageStorage;

/// <summary>Image storage abstraction (local wwwroot or Cloudflare R2). Used for stamp, usage-period, and report images.</summary>
public interface IImageStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string category, CancellationToken cancellationToken = default);
    string GetDisplayUrl(string storedIdentifier);
    Task DeleteAsync(string storedIdentifier, CancellationToken cancellationToken = default);
}
