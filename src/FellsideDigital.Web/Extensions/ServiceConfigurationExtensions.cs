using FellsideDigital.Web.Models;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Cryptography.X509Certificates;

namespace FellsideDigital.Web.Extensions;

public static class ServiceConfigurationExtensions
{
    public static IServiceCollection ConfigureHttp(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddHttpContextAccessor();
        services.AddAntiforgery();
        return services;
    }

    public static IServiceCollection ConfigureAuctionServices(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection ConfigureFormOptions(this IServiceCollection services)
    {
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB upload limit
        });

        return services;
    }

    public static IServiceCollection ConfigureDataProtection(this IServiceCollection services,
                                                             string keysFolder,
                                                             string certPath,
                                                             string certPassword)
    {
        var dataProtectionBuilder = services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
            .SetApplicationName("FellsideDigital");

        // Load and protect keys with certificate (if available)
        if (File.Exists(certPath))
        {
            var cert = X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);
            dataProtectionBuilder.ProtectKeysWithCertificate(cert);
        }

        return services;
    }

    public static IServiceCollection ConfigureSession(this IServiceCollection services)
    {
        services.AddSession(options =>
        {
            options.Cookie.Name = ".FellsideDigital.Session";
            options.IdleTimeout = TimeSpan.FromHours(1);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        return services;
    }

    public static IServiceCollection ConfigureEmailService(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<EmailSettings>()
            .Bind(config.GetSection("Email"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<EmailService>();
        return services;
    }

    public static IServiceCollection ConfigureInvitationServices(this IServiceCollection services)
    {
        services.AddScoped<IInvitationService, InvitationService>();
        return services;
    }

    public static IServiceCollection ConfigureStorageService(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<StorageSettings>(config.GetSection("Storage"));
        services.AddSingleton<IStorageService, S3StorageService>();
        return services;
    }

    public static IServiceCollection ConfigurePortalServices(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectTimelineService, ProjectTimelineService>();
        services.AddScoped<IProjectNoteService, ProjectNoteService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IProjectDocumentService, ProjectDocumentService>();
        services.AddScoped<IHeroProjectService, HeroProjectService>();
        services.AddScoped<LayoutStateService>();
        services.AddScoped<PortalPreviewState>();
        return services;
    }

    public static ILoggingBuilder ConfigureLogging(this ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddConsole();
        return logging;
    }
}
