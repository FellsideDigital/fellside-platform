namespace FellsideDigital.Web.Services;

/// <summary>
/// A pair of presigned URLs for a stored file: one that opens the file inline in the
/// browser (<see cref="ViewUrl"/>) and one that forces a download/save
/// (<see cref="DownloadUrl"/>). Both are time-limited.
/// </summary>
public sealed record FileLinks(string ViewUrl, string DownloadUrl);
