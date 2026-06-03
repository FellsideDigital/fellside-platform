using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class EnquiryServiceTests(PostgresFixture fx)
{
    private static ContactEnquiry NewEnquiry(string name = "Ada") => new()
    {
        Name = name,
        Email = $"{name}@example.com",
        ServiceType = "Website",
        Message = "I would like a new website please.",
    };

    [Fact]
    public async Task CreateAsync_persists_the_enquiry_unread()
    {
        await using var db = fx.CreateContext();
        var sut = new EnquiryService(db);

        var created = await sut.CreateAsync(NewEnquiry());

        await using var verify = fx.CreateContext();
        var found = await verify.ContactEnquiries.FindAsync(created.Id);
        Assert.NotNull(found);
        Assert.Equal("Ada", found!.Name);
        Assert.False(found.IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_sets_the_read_flag()
    {
        await using var db = fx.CreateContext();
        var sut = new EnquiryService(db);
        var created = await sut.CreateAsync(NewEnquiry("Grace"));

        await sut.MarkAsReadAsync(created.Id);

        await using var verify = fx.CreateContext();
        var found = await verify.ContactEnquiries.FindAsync(created.Id);
        Assert.True(found!.IsRead);
    }

    [Fact]
    public async Task GetAllAsync_returns_newest_first()
    {
        await using var db = fx.CreateContext();
        var sut = new EnquiryService(db);

        var older = NewEnquiry("Older");
        older.SubmittedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var newer = NewEnquiry("Newer");
        newer.SubmittedAt = DateTimeOffset.UtcNow;
        await sut.CreateAsync(older);
        await sut.CreateAsync(newer);

        var all = await sut.GetAllAsync();

        var newerIndex = all.FindIndex(e => e.Id == newer.Id);
        var olderIndex = all.FindIndex(e => e.Id == older.Id);
        Assert.True(newerIndex >= 0 && olderIndex >= 0);
        Assert.True(newerIndex < olderIndex, "Newer enquiry should be ordered before older.");
    }
}
