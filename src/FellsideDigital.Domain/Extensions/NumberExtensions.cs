namespace FellsideDigital.Domain.Extensions;

public static class NumberExtensions
{
    /// <summary>
    /// Formats a day number as an English ordinal, e.g. 1 → "1st", 2 → "2nd", 18 → "18th".
    /// </summary>
    public static string ToOrdinal(this int number)
    {
        // 11th–13th are exceptions to the usual last-digit rule.
        var suffix = (number % 100) is >= 11 and <= 13
            ? "th"
            : (number % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            };
        return $"{number}{suffix}";
    }
}
