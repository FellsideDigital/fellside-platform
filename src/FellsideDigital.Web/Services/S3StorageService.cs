using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FellsideDigital.Web.Models;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Web.Services;

public sealed class S3StorageService : IStorageService, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly StorageSettings _settings;
    private readonly ILogger<S3StorageService> _logger;

    private static readonly Dictionary<string, string> ContentTypes = new()
    {
        [".pdf"]  = "application/pdf",
        [".png"]  = "image/png",
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
    };

    public S3StorageService(IOptions<StorageSettings> settings, ILogger<S3StorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ForcePathStyle = true, // required for MinIO and most non-AWS providers
        };

        if (!string.IsNullOrWhiteSpace(_settings.ServiceUrl))
            config.ServiceURL = _settings.ServiceUrl;
        else
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region);

        _client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, config);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName  = _settings.BucketName,
            Key         = key,
            InputStream = content,
            ContentType = contentType,
            // Required for MinIO — it doesn't support chunked/streaming signatures
            //DisablePayloadSigning = true,
        };

        await _client.PutObjectAsync(request, ct);
        _logger.LogInformation("Uploaded {Key} to bucket {Bucket}", key, _settings.BucketName);
        return key;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _client.DeleteObjectAsync(_settings.BucketName, key, ct);
        _logger.LogInformation("Deleted {Key} from bucket {Bucket}", key, _settings.BucketName);
    }

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key        = key,
            Expires    = DateTime.UtcNow.Add(expiry),
            Verb       = HttpVerb.GET,
        };

        var url = _client.GetPreSignedURL(request);

        // When ServiceUrl is an internal Docker hostname (e.g. http://minio:9000),
        // rewrite the host so the browser can reach it via the public URL.
        if (!string.IsNullOrWhiteSpace(_settings.PublicUrl) &&
            !string.IsNullOrWhiteSpace(_settings.ServiceUrl))
        {
            url = url.Replace(
                _settings.ServiceUrl.TrimEnd('/'),
                _settings.PublicUrl.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase);
        }

        return Task.FromResult(url);
    }

    public void Dispose() => _client.Dispose();
}
