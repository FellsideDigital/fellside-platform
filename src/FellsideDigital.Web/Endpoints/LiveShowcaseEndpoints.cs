using FellsideDigital.Web.Services.Live;

namespace FellsideDigital.Web.Endpoints;

public static class LiveShowcaseEndpoints
{
    /// <summary>Renders the QR that points phones at the public /live/join page.</summary>
    public static void MapLiveShowcase(this WebApplication app)
    {
        app.MapGet("/api/live/qr.svg", (HttpContext ctx, IConfiguration cfg) =>
        {
            var baseUrl = (cfg["PUBLIC_BASE_URL"] ?? $"{ctx.Request.Scheme}://{ctx.Request.Host}").TrimEnd('/');
            var svg = LiveQrCode.Svg($"{baseUrl}/live/join");
            return Results.Content(svg, "image/svg+xml");
        });
    }
}
