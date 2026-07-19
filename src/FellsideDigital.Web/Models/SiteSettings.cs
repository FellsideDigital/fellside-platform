namespace FellsideDigital.Web.Models;

/// <summary>
/// Site-wide settings that can't be derived from the current request — used by
/// background work (e.g. the invoice automation worker) to build absolute portal
/// links where no <c>NavigationManager</c> exists.
/// </summary>
public class SiteSettings
{
    public string PublicBaseUrl { get; set; } = "https://fellsidedigital.co.uk";

    public string PortalProjectUrl(Guid projectId) =>
        $"{PublicBaseUrl.TrimEnd('/')}/Portal/Projects/{projectId}";
}
