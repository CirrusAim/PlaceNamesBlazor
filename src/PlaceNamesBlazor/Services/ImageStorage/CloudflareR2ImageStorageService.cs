using Amazon.S3;
using Amazon.S3.Model;

namespace PlaceNamesBlazor.Services.ImageStorage;

/// <summary>Stores images in Cloudflare R2 (S3-compatible). DB stores object key; display URL from PublicBaseUrl.</summary>
public class CloudflareR2ImageStorageService : IImageStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;

    public CloudflareR2ImageStorageService(IConfiguration configuration)
    {
        var accountId = configuration["ImageStorage:Cloudflare:AccountId"] ?? "";
        var accessKey = configuration["ImageStorage:Cloudflare:AccessKeyId"] ?? "";
        var secretKey = configuration["ImageStorage:Cloudflare:SecretAccessKey"] ?? "";
        _bucketName = configuration["ImageStorage:Cloudflare:BucketName"] ?? "place-names-images";
        // PublicBaseUrl is the R2 public bucket URL; also accept PublicUrl (e.g. Render env ImageStorage__Cloudflare__PublicUrl)
        var baseUrl = configuration["ImageStorage:Cloudflare:PublicBaseUrl"]
            ?? configuration["ImageStorage:Cloudflare:PublicUrl"]
            ?? "";
        _publicBaseUrl = baseUrl.TrimEnd('/');

        var endpoint = string.IsNullOrEmpty(accountId)
            ? null
            : $"https://{accountId.Trim()}.r2.cloudflarestorage.com";
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };
        _s3 = string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(accessKey.Trim(), secretKey.Trim(), config);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string category, CancellationToken cancellationToken = default)
    {
        var (data, ext) = await ImageMagicBytesValidator.ValidateAndReadAsync(content, ImageMagicBytesValidator.DefaultMaxSizeBytes, cancellationToken);
        var categoryDir = string.IsNullOrEmpty(category) ? "misc" : category.Trim().ToLowerInvariant();
        var key = $"{categoryDir}/{Guid.NewGuid():N}{ext}";

        // R2: DisablePayloadSigning avoids chunked signing which R2 does not support.
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = new MemoryStream(data),
            ContentType = GetContentType(ext),
            AutoCloseStream = false,
            DisablePayloadSigning = true
        };
        await _s3.PutObjectAsync(request, cancellationToken);
        return key;
    }

    public string GetDisplayUrl(string storedIdentifier)
    {
        if (string.IsNullOrWhiteSpace(storedIdentifier))
            return string.Empty;
        // R2 keys are "stamp/guid.jpg" (no "images/" prefix). DB may have "images/stamp/..." from local storage; normalize so URL matches R2.
        var key = storedIdentifier.TrimStart('/').Replace('\\', '/');
        if (key.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            key = key.Substring("images/".Length);
        return string.IsNullOrEmpty(_publicBaseUrl)
            ? key
            : _publicBaseUrl + "/" + key;
    }

    public async Task DeleteAsync(string storedIdentifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedIdentifier))
            return;
        var key = storedIdentifier.Trim().Replace('\\', '/');
        if (key.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            key = key.Substring("images/".Length);
        try
        {
            await _s3.DeleteObjectAsync(_bucketName, key, cancellationToken);
        }
        catch { /* ignore delete failure */ }
    }

    private static string GetContentType(string ext) => ext.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".bmp" => "image/bmp",
        _ => "application/octet-stream"
    };
}
