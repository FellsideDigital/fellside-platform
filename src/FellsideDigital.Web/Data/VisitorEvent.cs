namespace FellsideDigital.Web.Data;

/// <summary>
/// An anonymous, consent-gated record of a single page visit, used for aggregate
/// marketing analytics. Deliberately holds no directly identifying data: the visitor
/// is keyed only by a random <see cref="SessionId"/> and a salted one-way IP hash —
/// the raw IP address is never stored.
/// </summary>
public class VisitorEvent
{
    public Guid Id { get; set; }

    /// <summary>Random per-browser id from a first-party cookie. Not linked to any account.</summary>
    public Guid SessionId { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public string Path { get; set; } = "";

    // Derived server-side from the User-Agent (coarse classification only).
    public string Platform { get; set; } = "Other";   // iOS | Android | Desktop | Other (via DeviceDetector)
    public string Browser { get; set; } = "Other";

    /// <summary>Salted SHA-256 of the IP, truncated. For counting uniques only; not reversible to an IP.</summary>
    public string? IpHash { get; set; }

    /// <summary>ISO country code when a fronting proxy/CDN supplies it (e.g. CF-IPCountry); otherwise null.</summary>
    public string? Country { get; set; }

    // Browser-exposed signals the visitor's device reports (collected client-side after consent).
    public string? Language { get; set; }
    public string? Timezone { get; set; }
    public int? ScreenWidth { get; set; }
    public int? ScreenHeight { get; set; }
    public int? ViewportWidth { get; set; }
    public int? ViewportHeight { get; set; }

    // Marketing attribution.
    public string? Referrer { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }

    // Engagement.
    public int? EngagementSeconds { get; set; }
    public int? ScrollDepthPercent { get; set; }
}
