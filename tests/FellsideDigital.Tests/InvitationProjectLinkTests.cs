using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class InvitationProjectLinkTests(PostgresFixture fx)
{
    // NavigationManager/IEmailService are only used by CreateInvitationAsync, not by
    // AcceptInvitationAsync, so the accept path can be tested with those deps null.
    private static InvitationService Sut(FellsideDigitalDbContext db)
        => new(db, emailService: null!, navigationManager: null!,
               logger: new Microsoft.Extensions.Logging.Abstractions.NullLogger<InvitationService>());

    private static async Task<(string userId, Guid projectId, Guid invitationId)> SeedAsync(
        FellsideDigitalDbContext db, bool isPrimary, string? existingPrimary = null)
    {
        var admin = new ApplicationUser { UserName = $"a{Guid.NewGuid():N}@x.io", Email = "a@x.io" };
        var user = new ApplicationUser { UserName = $"u{Guid.NewGuid():N}@x.io", Email = "u@x.io" };
        db.Users.AddRange(admin, user);
        var project = new ClientProject
        {
            Name = "P", Description = "D", Status = ProjectStatus.Pending, Type = ProjectType.Website,
            ClientId = existingPrimary, CreatedByAdminId = admin.Id,
        };
        db.ClientProjects.Add(project);
        var invite = new ClientInvitation
        {
            Id = Guid.NewGuid(), Token = Guid.NewGuid().ToString("N"), Email = "u@x.io",
            FirstName = "U", LastName = "Ser", Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedByUserId = admin.Id,
            ProjectId = project.Id, IsPrimaryClient = isPrimary,
        };
        db.ClientInvitations.Add(invite);
        await db.SaveChangesAsync();
        return (user.Id, project.Id, invite.Id);
    }

    [Fact]
    public async Task Accept_primary_invite_sets_client_when_project_has_none()
    {
        await using var db = fx.CreateContext();
        var (userId, projectId, inviteId) = await SeedAsync(db, isPrimary: true);

        await Sut(db).AcceptInvitationAsync(inviteId, userId);

        var project = await db.ClientProjects.Include(p => p.Members).SingleAsync(p => p.Id == projectId);
        Assert.Equal(userId, project.ClientId);
        Assert.Empty(project.Members);
    }

    [Fact]
    public async Task Accept_collaborator_invite_adds_member()
    {
        await using var db = fx.CreateContext();
        var (userId, projectId, inviteId) = await SeedAsync(db, isPrimary: false);

        await Sut(db).AcceptInvitationAsync(inviteId, userId);

        var project = await db.ClientProjects.Include(p => p.Members).SingleAsync(p => p.Id == projectId);
        Assert.Null(project.ClientId);
        Assert.Contains(project.Members, m => m.UserId == userId);
    }

    [Fact]
    public async Task Accept_primary_invite_falls_back_to_member_when_primary_taken()
    {
        await using var db = fx.CreateContext();
        var occupant = new ApplicationUser { UserName = $"o{Guid.NewGuid():N}@x.io", Email = "o@x.io" };
        db.Users.Add(occupant); await db.SaveChangesAsync();
        var (userId, projectId, inviteId) = await SeedAsync(db, isPrimary: true, existingPrimary: occupant.Id);

        await Sut(db).AcceptInvitationAsync(inviteId, userId);

        var project = await db.ClientProjects.Include(p => p.Members).SingleAsync(p => p.Id == projectId);
        Assert.Equal(occupant.Id, project.ClientId);
        Assert.Contains(project.Members, m => m.UserId == userId);
    }
}
