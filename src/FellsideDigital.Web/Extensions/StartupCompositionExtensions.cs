using FellsideDigital.Web.Components;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Data.Seeding;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FellsideDigital.Web.Extensions;

public static class StartupCompositionExtensions
{
    public static IServiceCollection AddFellsideDigitalPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services
            .ConfigureDatabase(configuration)
            .ConfigureAuthentication()
            .ConfigureHttp()
            .ConfigureFormOptions()
            .ConfigureEmailService(configuration)
            .ConfigureStorageService(configuration)
            .ConfigureInvitationServices()
            .ConfigurePortalServices();

        // Required when running behind reverse proxies/load balancers (containers, cloud).
        // The known-proxy/known-network allowlists are cleared because Railway's edge proxy
        // has no fixed/announced IP, so the forwarded headers can't be pinned to a specific
        // source. The residual host-header risk that this would otherwise open up (e.g. link
        // poisoning via NavigationManager) is bounded by the AllowedHosts allowlist, which
        // rejects unexpected Host headers before they reach the app. ForwardLimit defaults to
        // 1, matching Railway's single proxy hop.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static async Task ApplyStartupTasksAsync(this WebApplication app)
    {
        app.ApplyDatabaseMigrations<FellsideDigitalDbContext>();

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ADMIN_EMAIL")) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ADMIN_PASSWORD")))
        {
            await AdminUserSeeder.SeedAdminAsync(app.Services);
        }

        // Demo clients/projects for the marketing hero carousel — development only, idempotent.
        if (app.Environment.IsDevelopment())
        {
            await DemoDataSeeder.SeedDemoProjectsAsync(app.Services);
        }
    }

    public static WebApplication UseFellsideDigitalPlatform(this WebApplication app)
    {
        app.UseForwardedHeaders();

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStatusCodePagesWithReExecute("/not-found");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.MapQrRedirects();
        app.MapScreenshotMedia();

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();

        return app;
    }

    private static void MapScreenshotMedia(this WebApplication app)
    {
        // Streams public project screenshots through the app's own origin (HTTPS), so they
        // aren't blocked as mixed content when storage is an HTTP endpoint (e.g. dev MinIO),
        // and so the storage bucket isn't exposed directly. Restricted to the "screenshots/"
        // prefix — private objects (e.g. invoices) are never reachable here.
        app.MapGet("/media/{**key}", async (string key, HttpContext ctx, IStorageService storage, CancellationToken ct) =>
        {
            if (!key.StartsWith("screenshots/", StringComparison.Ordinal))
                return Results.NotFound();

            var obj = await storage.GetObjectAsync(key, ct);
            if (obj is null) return Results.NotFound();

            ctx.Response.Headers.CacheControl = "public, max-age=3600";
            return Results.Stream(obj.Content, obj.ContentType);
        });
    }

    private static void MapQrRedirects(this WebApplication app)
    {
        app.MapGet("/q/{source}", async (string source, IQrLeadService qrLeads, HttpContext ctx) =>
        {
            var scan = await qrLeads.RecordScanAsync(
                source,
                ctx.Connection.RemoteIpAddress?.ToString(),
                ctx.Request.Headers.UserAgent.ToString());

            return Results.Redirect($"/scan?from={scan.Source}&ref={scan.Id}");
        });
    }
}
