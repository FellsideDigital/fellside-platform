using FellsideDigital.UI.Enums;

namespace FellsideDigital.UI.Components.Buttons;

internal static class ButtonStyles
{
    const string BaseClasses = "inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold transition-all disabled:opacity-50 disabled:cursor-not-allowed";

    private static readonly Dictionary<(ButtonColor, ButtonStyle), string> Map = new()
    {
        // Accent
        [(ButtonColor.Accent, ButtonStyle.Solid)] =
            $"bg-accent px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-accent/70 {BaseClasses}",
        [(ButtonColor.Accent, ButtonStyle.Outline)] =
            $"bg-transparent px-4 py-2.5 text-sm font-semibold text-accent ring-1 ring-accent hover:bg-accent/10 {BaseClasses}",
        [(ButtonColor.Accent, ButtonStyle.Text)] =
            "text-sm font-medium text-accent hover:text-accent/70 transition-colors disabled:opacity-50",

        // Muted
        [(ButtonColor.Muted, ButtonStyle.Solid)] =
            $"bg-gray-100 dark:bg-neutral-800 px-4 py-2.5 text-sm font-semibold text-gray-700 dark:text-neutral-200 shadow-sm hover:bg-gray-200 dark:hover:bg-neutral-700 {BaseClasses}",
        [(ButtonColor.Muted, ButtonStyle.Outline)] =
            $"bg-white dark:bg-white/5 px-4 py-2.5 text-sm font-semibold text-gray-700 dark:text-neutral-200 ring-1 ring-gray-200 dark:ring-white/10 hover:bg-gray-50 dark:hover:bg-white/10 {BaseClasses}",
        [(ButtonColor.Muted, ButtonStyle.Text)] =
            "text-sm font-medium text-muted hover:text-accent transition-colors disabled:opacity-50",

        // Danger
        [(ButtonColor.Danger, ButtonStyle.Solid)] =
            $"bg-red-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-red-500 {BaseClasses}",
        [(ButtonColor.Danger, ButtonStyle.Outline)] =
            $"bg-transparent px-4 py-2.5 text-sm font-semibold text-red-600 ring-1 ring-red-600 hover:bg-red-600/10 {BaseClasses}",
        [(ButtonColor.Danger, ButtonStyle.Text)] =
            "text-sm font-medium text-red-600 hover:text-red-500 transition-colors disabled:opacity-50",
    };

    internal static string Get(ButtonColor color, ButtonStyle style, bool fullWidth = false, string? extra = null)
    {
        var classes = Map[(color, style)];
        if (fullWidth) classes = "w-full justify-center " + classes;
        if (!string.IsNullOrEmpty(extra)) classes += " " + extra;
        return classes;
    }
}
