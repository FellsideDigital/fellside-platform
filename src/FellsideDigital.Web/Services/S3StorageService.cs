using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FellsideDigital.Web.Models;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Web.Services;

public sealed class S3StorageService : IStorageService, IDisposable
{
    private readonly AmazonS3Client _client;        // real operations (internal endpoint)
    private readonly AmazonS3Client _presignClient; // generates browser URLs (public endpoint)
    private readonly StorageSettings _settings;
    private readonly ILogger<S3StorageService> _logger;

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

        // Presigned URLs are signed (SigV4) against the endpoint host, so they must be
        // signed with the PUBLIC host the browser will actually use — rewriting the host
        // afterwards breaks the signature. When PublicUrl differs from the internal
        // ServiceUrl (e.g. minio:9000 vs localhost:9000), use a separate client whose
        // endpoint is the public host. Signing is a local operation, so this client never
        // needs to reach that host. On AWS/Railway PublicUrl is empty and the main client
        // already signs against the correct public endpoint.
        if (!string.IsNullOrWhiteSpace(_settings.PublicUrl) &&
            !string.Equals(_settings.PublicUrl, _settings.ServiceUrl, StringComparison.OrdinalIgnoreCase))
        {
            var presignConfig = new AmazonS3Config
            {
                ForcePathStyle = true,
                ServiceURL = _settings.PublicUrl,
            };
            _presignClient = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, presignConfig);
        }
        else
        {
            _presignClient = _client;
        }
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

    public async Task<StorageObject?> GetObjectAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_settings.BucketName, key, ct);
            var contentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? "application/octet-stream"
                : response.Headers.ContentType;
            return new StorageObject(response.ResponseStream, contentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
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

        // Sign against the public endpoint so the URL is valid when the browser uses it.
        var url = _presignClient.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public void Dispose()
    {
        if (!ReferenceEquals(_presignClient, _client))
            _presignClient.Dispose();
        _client.Dispose();
    }
}
