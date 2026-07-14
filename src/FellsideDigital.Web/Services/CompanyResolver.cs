using System.Globalization;

namespace FellsideDigital.Web.Services;

/// <summary>Derives a display company name from an email domain. Returns null for
/// generic mailbox providers or unparseable addresses.</summary>
public static class CompanyResolver
{
    private static readonly HashSet<string> GenericDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com", "outlook.com", "hotmail.com", "hotmail.co.uk",
        "live.com", "yahoo.com", "yahoo.co.uk", "icloud.com", "me.com", "mac.com",
        "aol.com", "proton.me", "protonmail.com", "gmx.com", "mail.com", "msn.com"
    };

    private static readonly HashSet<string> SecondLevel = new(StringComparer.OrdinalIgnoreCase)
    {
        "co", "com", "org", "net", "ac", "gov", "edu", "ltd", "plc"
    };

    public static string? Resolve(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var at = email.LastIndexOf('@');
        if (at <= 0 || at == email.Length - 1) return null;

        var domain = email[(at + 1)..].Trim().ToLowerInvariant();
        if (domain.Length == 0 || GenericDomains.Contains(domain)) return null;

        var labels = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length < 2) return null;

        var name = labels.Length >= 3 && SecondLevel.Contains(labels[^2])
            ? labels[^3]
            : labels[^2];

        if (name.Length == 0) return null;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
    }
}
