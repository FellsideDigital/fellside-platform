namespace FellsideDigital.Web.Services;

public interface IStorageService
{
    /// <summary>Uploads a stream and returns the object key.</summary>
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>Permanently deletes an object by key.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>Returns a time-limited presigned GET URL for the given key.</summary>
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>Opens an object for reading (content + content type), or null if it doesn't exist.</summary>
    Task<StorageObject?> GetObjectAsync(string key, CancellationToken ct = default);
}

/// <summary>A readable storage object and its content type.</summary>
public sealed record StorageObject(Stream Content, string ContentType);
