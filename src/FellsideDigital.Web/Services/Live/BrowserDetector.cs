namespace FellsideDigital.Web.Services.Live;

/// <summary>
/// Classifies a browser user-agent string into a coarse browser bucket
/// ("Edge", "Opera", "Firefox", "Chrome", "Safari", "Other"). Pure and
/// order-sensitive: Edge/Opera/Chrome all carry a "Chrome" token and Chrome
/// carries a "Safari" token, so the more specific brands are matched first.
/// Sibling of <see cref="DeviceDetector"/>.
/// </summary>
public static class BrowserDetector
{
    public const string Edge = "Edge";
    public const string Opera = "Opera";
    public const string Firefox = "Firefox";
    public const string Chrome = "Chrome";
    public const string Safari = "Safari";
    public const string Other = "Other";

    public static string Classify(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return Other;

        var ua = userAgent.ToLowerInvariant();

        if (ua.Contains("edg")) return Edge;                      // "Edg", "EdgA", "EdgiOS"
        if (ua.Contains("opr") || ua.Contains("opera")) return Opera;
        if (ua.Contains("firefox") || ua.Contains("fxios")) return Firefox;
        if (ua.Contains("chrome") || ua.Contains("crios")) return Chrome;
        if (ua.Contains("safari")) return Safari;

        return Other;
    }
}
