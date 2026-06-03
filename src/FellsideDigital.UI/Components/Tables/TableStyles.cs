namespace FellsideDigital.UI.Components.Tables;

/// <summary>
/// Canonical Tailwind class strings for data tables, centralising the styling that was
/// previously copy-pasted into every admin/portal table's table/head-row/body markup.
/// </summary>
public static class TableStyles
{
    /// <summary>The <c>&lt;table&gt;</c> element.</summary>
    public const string Table = "min-w-full divide-y divide-gray-100 dark:divide-white/5";

    /// <summary>The header <c>&lt;tr&gt;</c> inside <c>&lt;thead&gt;</c>.</summary>
    public const string HeadRow = "bg-gray-50/80 dark:bg-white/[0.02]";

    /// <summary>The <c>&lt;tbody&gt;</c> element.</summary>
    public const string Body = "divide-y divide-gray-100 dark:divide-white/5";

    /// <summary>A body <c>&lt;tr&gt;</c> with the standard hover treatment.</summary>
    public const string Row = "hover:bg-gray-50/50 dark:hover:bg-white/[0.02] transition-colors";
}
