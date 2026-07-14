using FellsideDigital.Web.Services.Live;

namespace FellsideDigital.Tests;

public class DeviceDetectorTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit", "iOS")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X)", "iOS")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit", "Android")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", "Desktop")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)", "Desktop")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64)", "Desktop")]
    public void Classifies_known_agents(string ua, string expected)
    {
        Assert.Equal(expected, DeviceDetector.Classify(ua));
    }

    [Fact]
    public void Android_wins_over_its_linux_token()
    {
        // Android UAs contain "Linux" — must not be classified as Desktop.
        Assert.Equal("Android", DeviceDetector.Classify("Mozilla/5.0 (Linux; Android 13)"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SomeUnknownBot/1.0")]
    public void Unknown_or_empty_is_other(string? ua)
    {
        Assert.Equal("Other", DeviceDetector.Classify(ua));
    }
}
