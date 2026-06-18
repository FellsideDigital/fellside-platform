using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration config)
    {
        // Resolve connection string - Railway provides DATABASE_URL as a postgres:// URI.
        // Standard .NET env var override (ConnectionStrings__DefaultConnection) also works.
        var connectionString = config.GetConnectionString("DefaultConnection");

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(databaseUrl))
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');

            // TLS to the database. Railway's managed Postgres presents a self-signed certificate
            // on the project-private network, so full chain validation (VerifyFull) isn't possible
            // without their CA — hence the default trusts the server cert. On any platform that
            // exposes a CA, set DB_SSL_MODE=VerifyFull (and DB_TRUST_SERVER_CERT=false) to get a
            // fully authenticated channel without a code change.
            var sslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Require";
            var trustServerCert = Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERT") ?? "true";
            connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode={sslMode};Trust Server Certificate={trustServerCert}";
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        services.AddDbContext<FellsideDigitalDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        //services.AddDatabaseDeveloperPageExceptionFilter();
        return services;
    }
}