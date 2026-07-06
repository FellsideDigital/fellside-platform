using System.Text.Json;
using FellsideDigital.Web.Components.Pages.Marketing;

namespace FellsideDigital.Tests;

public class LocationSeoTests
{
    public static TheoryData<string> Slugs() => new("keswick", "penrith", "kendal", "carlisle");

    [Theory]
    [MemberData(nameof(Slugs))]
    public void Every_location_emits_parseable_json_ld(string slug)
    {
        var loc = LocationData.All.Single(l => l.Slug == slug);

        foreach (var json in new[]
                 {
                     LocationData.ServiceJson(loc),
                     LocationData.BreadcrumbJson(loc),
                     LocationData.FaqJson(loc),
                 })
        {
            using var doc = JsonDocument.Parse(json); // throws on invalid JSON
            Assert.True(doc.RootElement.TryGetProperty("@type", out _));
        }
    }
}
