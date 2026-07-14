using FellsideDigital.Web.Services;

namespace FellsideDigital.Web.Endpoints;

public static class VisitorAnalyticsEndpoints
{
    /// <summary>Name of the first-party cookie recording the visitor's consent choice.</summary>
    public const string ConsentCookie = "fd_consent";

    /// <summary>
    /// Consent-gated capture endpoint for anonymous visitor analytics. The client only
    /// beacons here after the visitor accepts; the server independently re-checks the
    /// consent cookie so a missing/declined choice is never recorded (defence in depth).
    /// </summary>
    public static void MapVisitorAnalytics(this WebApplication app)
    {
        app.MapPost("/api/analytics/visit", async (
            VisitorCapture capture,
            HttpContext ctx,
            IVisitorAnalyticsService analytics,
            ILogger<VisitorAnalyticsService> logger,
            CancellationToken ct) =>
        {
            // Hard gate: no valid consent cookie => store nothing.
            if (!ctx.Request.Cookies.TryGetValue(ConsentCookie, out var consent) ||
                !string.Equals(consent, "accepted", StringComparison.Ordinal))
            {
                return Results.NoContent();
            }

            if (capture.SessionId == Guid.Empty)
                return Results.NoContent();

            try
            {
                var context = new VisitorRequestContext
                {
                    IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = ctx.Request.Headers.UserAgent.ToString(),
                    // Populated only when a fronting CDN/proxy supplies it.
                    Country = ctx.Request.Headers["CF-IPCountry"].FirstOrDefault(),
                };

                await analytics.RecordAsync(capture, context, ct);
            }
            catch (Exception ex)
            {
                // Analytics must never disrupt a visitor's browsing; swallow and log.
                logger.LogError(ex, "Failed to record visitor analytics event");
            }

            return Results.NoContent();
        });
    }
}
