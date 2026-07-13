using FellsideDigital.Web.Services.Live;

namespace FellsideDigital.Tests;

public class LiveQrCodeTests
{
    [Fact]
    public void Svg_returns_an_svg_document()
    {
        var svg = LiveQrCode.Svg("https://fellsidedigital.co.uk/live/join");

        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }
}
