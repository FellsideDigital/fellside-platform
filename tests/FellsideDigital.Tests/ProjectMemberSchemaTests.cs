using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class ProjectMemberSchemaTests(PostgresFixture fx)
{
    [Fact]
    public async Task Project_persists_with_null_client_and_a_member()
    {
        await using var db = fx.CreateContext();

        var user = new ApplicationUser { UserName = $"m{Guid.NewGuid():N}@x.io", Email = "m@x.io" };
        db.Users.Add(user);

        var project = new ClientProject
        {
            Name = "No client yet",
            Description = "Created before the client existed.",
            Status = ProjectStatus.Pending,
            Type = ProjectType.Website,
            ClientId = null,
            CreatedByAdminId = user.Id, // any user id satisfies the restrict FK for this test
        };
        db.ClientProjects.Add(project);
        await db.SaveChangesAsync();

        db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = user.Id });
        await db.SaveChangesAsync();

        var reloaded = await db.ClientProjects
            .Include(p => p.Members)
            .SingleAsync(p => p.Id == project.Id);

        Assert.Null(reloaded.ClientId);
        Assert.Single(reloaded.Members);
        Assert.Equal(user.Id, reloaded.Members.First().UserId);
    }
}
