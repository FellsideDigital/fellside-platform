using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class ProjectServiceTests(PostgresFixture fx)
{
    private static ClientProject NewProject(ProjectStatus status = ProjectStatus.Pending) => new()
    {
        Name = "Acme Site",
        Description = "A website.",
        Status = status,
        Type = ProjectType.Website,
    };

    [Fact]
    public async Task GetProjectCountAsync_counts_all_projects_regardless_of_status()
    {
        await using var db = fx.CreateContext();
        // Service only touches the DbContext for the count; other deps are unused here.
        var sut = new ProjectService(db, storage: null!, timeline: null!);

        var baseline = await sut.GetProjectCountAsync();

        db.ClientProjects.Add(NewProject(ProjectStatus.Pending));
        db.ClientProjects.Add(NewProject(ProjectStatus.InProgress));
        db.ClientProjects.Add(NewProject(ProjectStatus.Completed));
        await db.SaveChangesAsync();

        var after = await sut.GetProjectCountAsync();

        Assert.Equal(baseline + 3, after);
    }
}
