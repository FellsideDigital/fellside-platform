using FellsideDigital.Web.Services.Live;

namespace FellsideDigital.Tests;

public class BrowserDetectorTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36 Edg/120.0", "Edge")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 Chrome/120.0 Safari/537.36 OPR/106.0", "Opera")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; rv:121.0) Gecko/20100101 Firefox/121.0", "Firefox")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 Chrome/120.0 Safari/537.36", "Chrome")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1 Version/17.0 Safari/605.1", "Safari")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0) AppleWebKit CriOS/120.0 Mobile Safari", "Chrome")]
    public void Classifies_known_browsers(string ua, string expected)
    {
        Assert.Equal(expected, BrowserDetector.Classify(ua));
    }

    [Fact]
    public void Edge_wins_over_its_chrome_and_safari_tokens()
    {
        // Edge UAs contain both "Chrome" and "Safari" — the brand must win.
        var ua = "Mozilla/5.0 AppleWebKit/537.36 Chrome/120.0 Safari/537.36 Edg/120.0";
        Assert.Equal("Edge", BrowserDetector.Classify(ua));
    }

    [Fact]
    public void Chrome_wins_over_its_safari_token()
    {
        var ua = "Mozilla/5.0 AppleWebKit/537.36 Chrome/120.0 Safari/537.36";
        Assert.Equal("Chrome", BrowserDetector.Classify(ua));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SomeUnknownBot/1.0")]
    public void Unknown_or_empty_is_other(string? ua)
    {
        Assert.Equal("Other", BrowserDetector.Classify(ua));
    }
}
