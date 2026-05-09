using FellsideDigital.Web.Components;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Data.Seeding;
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

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();

        return app;
    }
}
