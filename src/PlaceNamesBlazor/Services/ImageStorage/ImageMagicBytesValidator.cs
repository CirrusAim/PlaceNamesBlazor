namespace PlaceNamesBlazor.Services.ImageStorage;

/// <summary>Validates image uploads by magic bytes (JPEG, PNG, GIF, WebP, BMP). Rejects non-image or unsupported content.</summary>
public static class ImageMagicBytesValidator
{
    public const long DefaultMaxSizeBytes = 10L * 1024 * 1024; // 10 MB

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Gif87a = [0x47, 0x49, 0x46, 0x38, 0x37, 0x61];
    private static readonly byte[] Gif89a = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];
    private static readonly byte[] WebP = [0x52, 0x49, 0x46, 0x46]; // RIFF; WEBP at offset 8
    private static readonly byte[] Bmp = [0x42, 0x4D];

    /// <summary>Reads stream up to maxSizeBytes, validates magic bytes, returns content and extension. Throws if invalid or over size.</summary>
    public static async Task<(byte[] Content, string Extension)> ValidateAndReadAsync(Stream stream, long maxSizeBytes = DefaultMaxSizeBytes, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, maxSizeBytes - total)), cancellationToken)) > 0)
        {
            total += read;
            if (total > maxSizeBytes)
                throw new InvalidOperationException($"Image size exceeds the maximum allowed ({maxSizeBytes / (1024 * 1024)} MB).");
            ms.Write(buffer, 0, read);
        }
        var content = ms.ToArray();
        if (content.Length == 0)
            throw new InvalidOperationException("Empty file is not a valid image.");
        var ext = GetExtensionFromMagic(content);
        if (ext == null)
            throw new InvalidOperationException("Invalid or unsupported image format. Allowed: JPEG, PNG, GIF, WebP, BMP.");
        return (content, ext);
    }

    private static string? GetExtensionFromMagic(byte[] content)
    {
        if (content.Length < 12)
            return null;
        if (StartsWith(content, Jpeg)) return ".jpg";
        if (StartsWith(content, Png)) return ".png";
        if (StartsWith(content, Gif87a) || StartsWith(content, Gif89a)) return ".gif";
        if (StartsWith(content, Bmp)) return ".bmp";
        if (StartsWith(content, WebP) && content.Length >= 12 &&
            content[8] == 0x57 && content[9] == 0x45 && content[10] == 0x42 && content[11] == 0x50) return ".webp";
        return null;
    }

    private static bool StartsWith(byte[] content, byte[] prefix)
    {
        if (content.Length < prefix.Length) return false;
        for (var i = 0; i < prefix.Length; i++)
            if (content[i] != prefix[i]) return false;
        return true;
    }
}
