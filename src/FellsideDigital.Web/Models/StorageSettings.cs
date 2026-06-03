namespace FellsideDigital.Web.Models;

public class StorageSettings
{
    /// <summary>S3-compatible endpoint used by the SDK (can be internal, e.g. http://minio:9000).</summary>
    public string ServiceUrl { get; set; } = "";

    /// <summary>
    /// Public-facing base URL used when rewriting presigned URLs for the browser.
    /// Required when ServiceUrl is an internal Docker hostname (e.g. set to http://localhost:9000).
    /// Leave empty for Railway/AWS — ServiceUrl is already public in those cases.
    /// </summary>
    public string PublicUrl { get; set; } = "";

    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string BucketName { get; set; } = "documents";
    public string Region { get; set; } = "us-east-1";
    public int PresignedUrlExpiryMinutes { get; set; } = 60;
}
