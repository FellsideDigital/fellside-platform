using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class ProjectMemberServiceTests(PostgresFixture fx)
{
    private static ProjectService Sut(FellsideDigitalDbContext db)
        => new(db, storage: null!, new ProjectTimelineService(db));

    private static ApplicationUser NewUser() =>
        new() { UserName = $"u{Guid.NewGuid():N}@x.io", Email = "u@x.io" };

    private static async Task<(string adminId, ClientProject project)> SeedProjectAsync(
        FellsideDigitalDbContext db, string? clientId = null)
    {
        var admin = NewUser();
        db.Users.Add(admin);
        var project = new ClientProject
        {
            Name = "P", Description = "D",
            Status = ProjectStatus.Pending, Type = ProjectType.Website,
            ClientId = clientId, CreatedByAdminId = admin.Id,
        };
        db.ClientProjects.Add(project);
        await db.SaveChangesAsync();
        return (admin.Id, project);
    }

    [Fact]
    public async Task AddMemberAsync_grants_access_and_is_idempotent()
    {
        await using var db = fx.CreateContext();
        var member = NewUser(); db.Users.Add(member); await db.SaveChangesAsync();
        var (adminId, project) = await SeedProjectAsync(db);

        await Sut(db).AddMemberAsync(project.Id, member.Id, adminId);
        await Sut(db).AddMemberAsync(project.Id, member.Id, adminId); // duplicate

        var rows = await db.ProjectMembers.Where(m => m.ProjectId == project.Id).ToListAsync();
        Assert.Single(rows);

        var forMember = await Sut(db).GetForClientAsync(member.Id);
        Assert.Contains(forMember, p => p.Id == project.Id);
    }

    [Fact]
    public async Task RemoveMemberAsync_revokes_access()
    {
        await using var db = fx.CreateContext();
        var member = NewUser(); db.Users.Add(member); await db.SaveChangesAsync();
        var (adminId, project) = await SeedProjectAsync(db);
        await Sut(db).AddMemberAsync(project.Id, member.Id, adminId);

        await Sut(db).RemoveMemberAsync(project.Id, member.Id);

        var forMember = await Sut(db).GetForClientAsync(member.Id);
        Assert.DoesNotContain(forMember, p => p.Id == project.Id);
    }

    [Fact]
    public async Task GetForClientAsync_returns_projects_where_user_is_primary_or_member_once()
    {
        await using var db = fx.CreateContext();
        var user = NewUser(); db.Users.Add(user); await db.SaveChangesAsync();
        var (adminId, primaryProject) = await SeedProjectAsync(db, clientId: user.Id);
        var (_, memberProject) = await SeedProjectAsync(db);
        await Sut(db).AddMemberAsync(memberProject.Id, user.Id, adminId);

        var result = await Sut(db).GetForClientAsync(user.Id);

        Assert.Contains(result, p => p.Id == primaryProject.Id);
        Assert.Contains(result, p => p.Id == memberProject.Id);
        Assert.Equal(result.Select(p => p.Id).Distinct().Count(), result.Count);
    }

    [Fact]
    public async Task SetPrimaryClientAsync_sets_then_clears_and_demotes_existing_member()
    {
        await using var db = fx.CreateContext();
        var user = NewUser(); db.Users.Add(user); await db.SaveChangesAsync();
        var (adminId, project) = await SeedProjectAsync(db);
        await Sut(db).AddMemberAsync(project.Id, user.Id, adminId);

        await Sut(db).SetPrimaryClientAsync(project.Id, user.Id, adminId);
        var set = await db.ClientProjects.Include(p => p.Members).SingleAsync(p => p.Id == project.Id);
        Assert.Equal(user.Id, set.ClientId);
        Assert.Empty(set.Members); // promoting a member to primary removes the member row

        await Sut(db).SetPrimaryClientAsync(project.Id, null, adminId);
        var cleared = await db.ClientProjects.AsNoTracking().SingleAsync(p => p.Id == project.Id);
        Assert.Null(cleared.ClientId);
    }
}
