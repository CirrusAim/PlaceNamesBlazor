namespace PlaceNamesBlazor.Services.ImageStorage;

/// <summary>Stores images under wwwroot/images (or ImageStorage:Local:BasePath). For local dev without Cloudflare.</summary>
public class LocalImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly string _basePath;
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();

    public LocalImageStorageService(IWebHostEnvironment env, IConfiguration configuration)
    {
        _env = env;
        var localBase = configuration["ImageStorage:Local:BasePath"]?.Trim();
        _basePath = string.IsNullOrEmpty(localBase)
            ? Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "images")
            : Path.IsPathRooted(localBase) ? localBase : Path.Combine(_env.ContentRootPath, localBase);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string category, CancellationToken cancellationToken = default)
    {
        var (data, extension) = await ImageMagicBytesValidator.ValidateAndReadAsync(content, ImageMagicBytesValidator.DefaultMaxSizeBytes, cancellationToken);
        var relativeDir = string.IsNullOrEmpty(category) ? "misc" : category.Trim().ToLowerInvariant();
        var relativePath = Path.Combine(relativeDir, $"{Guid.NewGuid():N}{extension}").Replace('\\', '/');
        var fullPath = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await fs.WriteAsync(data.AsMemory(), cancellationToken);
        var stored = "images/" + relativePath;
        return stored;
    }

    public string GetDisplayUrl(string storedIdentifier)
    {
        if (string.IsNullOrWhiteSpace(storedIdentifier))
            return string.Empty;
        var path = storedIdentifier.TrimStart('/').Replace('\\', '/');
        if (path.Contains("..", StringComparison.Ordinal))
            return string.Empty;
        return path.StartsWith("images/", StringComparison.OrdinalIgnoreCase) ? "/" + path : "/images/" + path;
    }

    public Task DeleteAsync(string storedIdentifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedIdentifier))
            return Task.CompletedTask;
        var relative = storedIdentifier.TrimStart('/').Replace('\\', Path.DirectorySeparatorChar);
        if (relative.Contains("..", StringComparison.Ordinal) || relative.StartsWith("..") || Path.IsPathRooted(relative))
            return Task.CompletedTask;
        var fullPath = Path.Combine(_basePath, relative);
        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch { /* ignore delete failure */ }
        return Task.CompletedTask;
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;
        var name = Path.GetFileName(fileName);
        foreach (var c in InvalidChars)
            name = name.Replace(c, '_');
        return name;
    }
}
