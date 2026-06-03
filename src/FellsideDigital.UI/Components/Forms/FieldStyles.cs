namespace FellsideDigital.UI.Components.Forms;

/// <summary>
/// Canonical Tailwind class strings for form controls. Keeping these in one place
/// removes the duplicated input styling that was previously copy-pasted into every
/// admin/portal page (as a re-declared local <c>InputClass</c> const or inline markup).
/// </summary>
public static class FieldStyles
{
    /// <summary>Standard single-line input / select / date control styling.</summary>
    public const string Input =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-accent transition-shadow outline-none";

    /// <summary>Multi-line (textarea) styling — the standard input plus <c>resize-none</c>.</summary>
    public const string TextArea = Input + " resize-none";

    /// <summary>Validation message styling, matching the <c>ValidationMessage</c> usage across forms.</summary>
    public const string Error = "mt-1 text-xs text-red-600 dark:text-red-400";

    /// <summary>Appends extra utility classes to the standard input styling.</summary>
    public static string Extend(string? extra) =>
        string.IsNullOrWhiteSpace(extra) ? Input : $"{Input} {extra}";
}
