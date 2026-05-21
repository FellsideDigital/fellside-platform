using FellsideDigital.Web.Components;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Data.Seeding;
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

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();

        return app;
    }

    private static void MapQrRedirects(this WebApplication app)
    {
        var validSources = new HashSet<string> { "shirt", "card" };

        app.MapGet("/q/{source}", async (string source, FellsideDigitalDbContext db, HttpContext ctx) =>
        {
            var normalized = validSources.Contains(source.ToLower()) ? source.ToLower() : "unknown";

            var scan = new QrScan
            {
                Source    = normalized,
                IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
                UserAgent = ctx.Request.Headers.UserAgent.ToString(),
            };

            db.QrScans.Add(scan);
            await db.SaveChangesAsync();

            return Results.Redirect($"/scan?from={normalized}&ref={scan.Id}");
        });
    }
}
