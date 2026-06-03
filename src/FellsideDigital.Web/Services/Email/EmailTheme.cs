namespace FellsideDigital.Web.Services.Email;

/// <summary>
/// Single source of truth for transactional-email branding. Change the palette
/// or the shared building blocks here and every template updates. Templates must
/// not hardcode colours or re-declare button/table markup — use these helpers.
/// </summary>
internal static class EmailTheme
{
    // ── Palette ──────────────────────────────────────────────────────────────
    public const string HeaderBg    = "#0f172a"; // slate-900
    public const string PageBg      = "#f1f5f9"; // slate-100
    public const string Surface     = "#ffffff";
    public const string SurfaceMute = "#f8fafc"; // slate-50
    public const string Border      = "#e2e8f0"; // slate-200
    public const string BorderSoft  = "#f1f5f9"; // slate-100

    public const string Heading     = "#0f172a"; // slate-900
    public const string Body        = "#475569"; // slate-600
    public const string Muted       = "#64748b"; // slate-500
    public const string Faint       = "#94a3b8"; // slate-400

    /// <summary>blue-400 — accents, links, borders, highlights.</summary>
    public const string AccentLight = "#60a5fa";
    /// <summary>blue-400 — button fills (white text passes AA contrast).</summary>
    public const string AccentButton = "#60a5fa";
    /// <summary>blue-50 — soft accent surfaces.</summary>
    public const string AccentSoft  = "#eff6ff";
    /// <summary>blue-700 — accent text on light surfaces.</summary>
    public const string AccentDeep  = "#1d4ed8";

    /// <summary>ContentId for the inline logo attachment added in EmailService.</summary>
    public const string LogoContentId = "fellside-logo";

    // ── Building blocks ──────────────────────────────────────────────────────

    /// <summary>Primary call-to-action button.</summary>
    public static string Button(string url, string label) => $"""
        <a href="{url}" style="display:inline-block;background:{AccentButton};color:#ffffff;text-decoration:none;padding:12px 28px;border-radius:8px;font-weight:700;font-size:14px;letter-spacing:.2px;">
            {label}
        </a>
        """;

    /// <summary>A bordered key/value table from label→value pairs.</summary>
    public static string InfoTable(IEnumerable<(string Label, string Value)> rows)
    {
        var body = string.Concat(rows.Select(r => $"""
            <tr>
                <td style="padding:12px 14px;color:{Muted};font-size:13px;width:150px;border-bottom:1px solid {Border};vertical-align:top;">{r.Label}</td>
                <td style="padding:12px 14px;font-size:14px;color:{Heading};border-bottom:1px solid {Border};line-height:1.55;">{r.Value}</td>
            </tr>
            """));

        return $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:separate;border-spacing:0;margin:0 0 20px;background:{SurfaceMute};border:1px solid {Border};border-radius:10px;overflow:hidden;">
                {body}
            </table>
            """;
    }

    /// <summary>Wraps inner content in the full branded email shell.</summary>
    public static string Layout(string content) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:{PageBg};font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background:{PageBg};padding:32px 14px;">
                <tr><td align="center">
                    <table width="100%" cellpadding="0" cellspacing="0" style="max-width:620px;background:{Surface};border-radius:14px;overflow:hidden;border:1px solid {Border};box-shadow:0 1px 3px rgba(15,23,42,.07);">
                        <tr>
                            <td style="background:{HeaderBg};padding:20px 24px;">
                                <table role="presentation" cellpadding="0" cellspacing="0">
                                    <tr>
                                        <td style="vertical-align:middle;padding-right:10px;">
                                            <img src="cid:{LogoContentId}" width="32" height="32" alt="Fellside Digital" style="display:block;border:0;outline:none;text-decoration:none;width:32px;height:32px;" />
                                        </td>
                                        <td style="vertical-align:middle;">
                                            <span style="color:#ffffff;font-weight:700;font-size:18px;letter-spacing:-0.2px;">Fellside Digital</span>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        <tr>
                            <td style="padding:30px 24px 26px;">
                                {content}
                            </td>
                        </tr>
                        <tr>
                            <td style="padding:16px 24px;border-top:1px solid {BorderSoft};color:{Faint};font-size:12px;line-height:1.5;">
                                Fellside Digital · Cumbria, UK<br/>
                                This is an automated message, please do not reply.
                            </td>
                        </tr>
                    </table>
                </td></tr>
            </table>
        </body>
        </html>
        """;
}
