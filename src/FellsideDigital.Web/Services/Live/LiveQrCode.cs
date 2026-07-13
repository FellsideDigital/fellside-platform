using QRCoder;

namespace FellsideDigital.Web.Services.Live;

/// <summary>Renders a scannable QR code as an inline SVG for the big screen.</summary>
public static class LiveQrCode
{
    public static string Svg(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        return new SvgQRCode(data).GetGraphic(10, "#0f172a", "#ffffff", drawQuietZones: true);
    }
}
