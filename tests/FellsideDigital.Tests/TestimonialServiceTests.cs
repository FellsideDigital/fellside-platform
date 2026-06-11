using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class TestimonialServiceTests(PostgresFixture fx)
{
    private async Task<string> SeedUserAsync()
    {
        await using var db = fx.CreateContext();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"{Guid.NewGuid():N}@example.com",
            Email = $"{Guid.NewGuid():N}@example.com",
            FirstName = "Jane",
            LastName = "Cooper",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task SubmitOrUpdateAsync_creates_a_pending_testimonial()
    {
        var userId = await SeedUserAsync();
        await using var db = fx.CreateContext();
        var sut = new TestimonialService(db);

        await sut.SubmitOrUpdateAsync(userId, 5, "Brilliant work.", "Jane Cooper", "Director, Acme");

        await using var verify = fx.CreateContext();
        var saved = await sut.GetForUserAsync(userId);
        Assert.NotNull(saved);
        Assert.Equal(5, saved!.Rating);
        Assert.Equal("Brilliant work.", saved.Quote);
        Assert.Equal(TestimonialStatus.Pending, saved.Status);
        Assert.Null(saved.ApprovedAt);
    }

    [Fact]
    public async Task SubmitOrUpdateAsync_keeps_one_per_user_and_resets_to_pending()
    {
        var userId = await SeedUserAsync();
        await using var db = fx.CreateContext();
        var sut = new TestimonialService(db);

        await sut.SubmitOrUpdateAsync(userId, 5, "First take.", "Jane", "Director");
        await sut.SetStatusAsync((await sut.GetForUserAsync(userId))!.Id, TestimonialStatus.Approved);

        // Editing an already-approved testimonial sends it back for review.
        await sut.SubmitOrUpdateAsync(userId, 4, "Updated take.", "Jane", "Director");

        var all = await sut.GetAllAsync();
        var mine = all.Where(t => t.UserId == userId).ToList();
        Assert.Single(mine);
        Assert.Equal(4, mine[0].Rating);
        Assert.Equal("Updated take.", mine[0].Quote);
        Assert.Equal(TestimonialStatus.Pending, mine[0].Status);
        Assert.Null(mine[0].ApprovedAt);
    }

    [Fact]
    public async Task GetApprovedAsync_returns_only_approved()
    {
        var approvedUser = await SeedUserAsync();
        var pendingUser = await SeedUserAsync();
        await using var db = fx.CreateContext();
        var sut = new TestimonialService(db);

        await sut.SubmitOrUpdateAsync(approvedUser, 5, "Approved one.", "A", "Role");
        await sut.SubmitOrUpdateAsync(pendingUser, 5, "Pending one.", "P", "Role");
        await sut.SetStatusAsync((await sut.GetForUserAsync(approvedUser))!.Id, TestimonialStatus.Approved);

        var approved = await sut.GetApprovedAsync();

        Assert.Contains(approved, t => t.UserId == approvedUser);
        Assert.DoesNotContain(approved, t => t.UserId == pendingUser);
        Assert.All(approved, t => Assert.Equal(TestimonialStatus.Approved, t.Status));
    }

    [Fact]
    public async Task SetStatusAsync_approving_stamps_ApprovedAt()
    {
        var userId = await SeedUserAsync();
        await using var db = fx.CreateContext();
        var sut = new TestimonialService(db);
        await sut.SubmitOrUpdateAsync(userId, 5, "Great.", "A", "Role");

        await sut.SetStatusAsync((await sut.GetForUserAsync(userId))!.Id, TestimonialStatus.Approved);

        var saved = await sut.GetForUserAsync(userId);
        Assert.Equal(TestimonialStatus.Approved, saved!.Status);
        Assert.NotNull(saved.ApprovedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task SubmitOrUpdateAsync_rejects_out_of_range_rating(int rating)
    {
        var userId = await SeedUserAsync();
        await using var db = fx.CreateContext();
        var sut = new TestimonialService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SubmitOrUpdateAsync(userId, rating, "A quote.", "A", "Role"));
    }

    [Fact]
    public async Task SubmitOrUpdateAsync_rejects_empty_quote()
    {
        var userId = await SeedUserAsync();
        await using var db = fx.CreateContext();
        var sut = new TestimonialService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SubmitOrUpdateAsync(userId, 5, "   ", "A", "Role"));
    }
}
