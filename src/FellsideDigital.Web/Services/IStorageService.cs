namespace FellsideDigital.Web.Services;

public interface IStorageService
{
    /// <summary>Uploads a stream and returns the object key.</summary>
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>Permanently deletes an object by key.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>Returns a time-limited presigned GET URL for the given key.</summary>
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);
}
