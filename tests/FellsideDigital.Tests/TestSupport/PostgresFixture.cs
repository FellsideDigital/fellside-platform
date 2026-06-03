using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FellsideDigital.Tests.TestSupport;

/// <summary>
/// Spins up a real PostgreSQL container once per test collection and applies the EF
/// migrations, so service tests run against production-shaped schema.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public FellsideDigitalDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FellsideDigitalDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new FellsideDigitalDbContext(options);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
