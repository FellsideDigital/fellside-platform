namespace FellsideDigital.Web.Services.Live;

/// <summary>
/// Classifies a browser user-agent string into a coarse device bucket for the
/// live showcase metrics ("iOS", "Android", "Desktop", "Other"). Pure and
/// order-sensitive: Android reports a Linux token, so it is matched first.
/// </summary>
public static class DeviceDetector
{
    public const string IOS = "iOS";
    public const string Android = "Android";
    public const string Desktop = "Desktop";
    public const string Other = "Other";

    public static string Classify(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return Other;

        var ua = userAgent.ToLowerInvariant();

        if (ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("ipod")) return IOS;
        if (ua.Contains("android")) return Android;
        if (ua.Contains("windows") || ua.Contains("macintosh") || ua.Contains("mac os x")
            || ua.Contains("cros") || ua.Contains("linux")) return Desktop;

        return Other;
    }
}
