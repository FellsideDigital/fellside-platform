using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class ProjectServiceTests(PostgresFixture fx)
{
    private static ClientProject NewProject(string adminId, ProjectStatus status = ProjectStatus.Pending) => new()
    {
        Name = "Acme Site",
        Description = "A website.",
        Status = status,
        Type = ProjectType.Website,
        CreatedByAdminId = adminId,
    };

    [Fact]
    public async Task GetProjectCountAsync_counts_all_projects_regardless_of_status()
    {
        await using var db = fx.CreateContext();
        // Service only touches the DbContext for the count; other deps are unused here.
        var sut = new ProjectService(db, storage: null!, timeline: null!);

        // CreatedByAdminId is a required (Restrict) FK, so the project needs a real admin.
        var admin = new ApplicationUser { UserName = $"a{Guid.NewGuid():N}@x.io", Email = "a@x.io" };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var baseline = await sut.GetProjectCountAsync();

        db.ClientProjects.Add(NewProject(admin.Id, ProjectStatus.Pending));
        db.ClientProjects.Add(NewProject(admin.Id, ProjectStatus.InProgress));
        db.ClientProjects.Add(NewProject(admin.Id, ProjectStatus.Completed));
        await db.SaveChangesAsync();

        var after = await sut.GetProjectCountAsync();

        Assert.Equal(baseline + 3, after);
    }

    [Fact]
    public async Task GetByIdAsync_hydrates_showcase_metrics_pipeline_and_integrations()
    {
        await using var db = fx.CreateContext();
        var sut = new ProjectService(db, storage: null!, timeline: null!);

        var admin = new ApplicationUser { UserName = $"a{Guid.NewGuid():N}@x.io", Email = "a@x.io" };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var project = NewProject(admin.Id);
        db.ClientProjects.Add(project);
        await db.SaveChangesAsync();

        db.ProjectMetrics.Add(new ProjectMetric { ProjectId = project.Id, Label = "Uptime", Value = "99.9%", DisplayOrder = 0 });
        db.ProjectPipelineSteps.Add(new ProjectPipelineStep { ProjectId = project.Id, Label = "Build", DisplayOrder = 0 });
        db.ProjectIntegrations.Add(new ProjectIntegration { ProjectId = project.Id, Name = "Stripe", DisplayOrder = 0 });
        await db.SaveChangesAsync();

        // Fresh context so nothing is served from the change tracker — the collections
        // must come from the query's Include clauses, not identity-map leakage.
        await using var readDb = fx.CreateContext();
        var readSut = new ProjectService(readDb, storage: null!, timeline: null!);

        var loaded = await readSut.GetByIdAsync(project.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Uptime", Assert.Single(loaded!.Metrics).Label);
        Assert.Equal("Build", Assert.Single(loaded.PipelineSteps).Label);
        Assert.Equal("Stripe", Assert.Single(loaded.Integrations).Name);
    }
}
