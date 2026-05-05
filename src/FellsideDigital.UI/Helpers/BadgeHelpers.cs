using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Components.Shared;

public static class BadgeHelpers
{
    public static string StatusLabel(ProjectStatus s) => s switch
    {
        ProjectStatus.InProgress => "In Progress",
        ProjectStatus.OnHold     => "On Hold",
        _                        => s.ToString()
    };

    public static string ProjectStatusBadge(ProjectStatus s) => s switch
    {
        ProjectStatus.InProgress => "bg-blue-50 text-blue-700 ring-1 ring-blue-600/20 dark:bg-blue-400/10 dark:text-blue-400",
        ProjectStatus.Completed  => "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/20 dark:bg-emerald-400/10 dark:text-emerald-400",
        ProjectStatus.Blocked    => "bg-red-50 text-red-700 ring-1 ring-red-600/20 dark:bg-red-400/10 dark:text-red-400",
        ProjectStatus.OnHold     => "bg-amber-50 text-amber-700 ring-1 ring-amber-600/20 dark:bg-amber-400/10 dark:text-amber-400",
        ProjectStatus.Pending    => "bg-slate-100 text-slate-600 ring-1 ring-slate-500/20 dark:bg-white/5 dark:text-neutral-400",
        _                        => ""
    };

    public static string InvoiceStatusBadge(InvoiceStatus s) => s switch
    {
        InvoiceStatus.Paid    => "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/20 dark:bg-emerald-400/10 dark:text-emerald-400",
        InvoiceStatus.Sent    => "bg-blue-50 text-blue-700 ring-1 ring-blue-600/20 dark:bg-blue-400/10 dark:text-blue-400",
        InvoiceStatus.Overdue => "bg-red-50 text-red-700 ring-1 ring-red-600/20 dark:bg-red-400/10 dark:text-red-400",
        InvoiceStatus.Draft   => "bg-slate-100 text-slate-600 ring-1 ring-slate-500/20 dark:bg-white/5 dark:text-neutral-400",
        _                     => ""
    };

    public static string InvitationStatusBadge(InvitationStatus s) => s switch
    {
        InvitationStatus.Pending  => "bg-amber-50 text-amber-700 ring-1 ring-amber-600/20 dark:bg-amber-400/10 dark:text-amber-400",
        InvitationStatus.Accepted => "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/20 dark:bg-emerald-400/10 dark:text-emerald-400",
        InvitationStatus.Expired  => "bg-slate-100 text-slate-600 ring-1 ring-slate-500/20 dark:bg-white/5 dark:text-neutral-400",
        InvitationStatus.Revoked  => "bg-red-50 text-red-700 ring-1 ring-red-600/20 dark:bg-red-400/10 dark:text-red-400",
        _                         => ""
    };

    public static string ProjectStatusDotColor(ProjectStatus s) => s switch
    {
        ProjectStatus.InProgress => "bg-blue-400",
        ProjectStatus.Completed  => "bg-emerald-400",
        ProjectStatus.Blocked    => "bg-red-400",
        ProjectStatus.OnHold     => "bg-amber-400",
        ProjectStatus.Pending    => "bg-slate-400 dark:bg-neutral-500",
        _                        => "bg-slate-400"
    };
}
