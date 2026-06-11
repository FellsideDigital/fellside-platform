using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Components.Shared;

/// <summary>
/// Single source of truth for how a <see cref="TimelineEventType"/> is rendered (icon + tone).
/// Keeping the per-type mapping here — rather than as a switch inside Razor — means adding a
/// new event type is a one-line change and every timeline renders it consistently. The
/// human-readable text comes from each event's snapshotted <c>Summary</c>, not from here.
/// </summary>
public static class TimelineEventPresenter
{
    public readonly record struct Visual(string IconPath, string IconWrapClasses, string IconColorClasses);

    // Heroicons (outline) path data, matched to the tone of each event family.
    private const string IconBolt    = "M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75z";
    private const string IconCheck   = "M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z";
    private const string IconNote    = "M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.087.16 2.185.283 3.293.369V21l4.184-4.183a1.14 1.14 0 0 1 .778-.332 48.294 48.294 0 0 0 5.83-.498c1.585-.233 2.708-1.626 2.708-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0 0 12 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018Z";
    private const string IconInvoice = "M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75 2.25 2.25 0 0 0-.1-.664m-5.8 0A2.251 2.251 0 0 1 13.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25Z";
    private const string IconFlag    = "M3 3v1.5M3 21v-6m0 0 2.77-.693a9 9 0 0 1 6.208.682l.108.054a9 9 0 0 0 6.086.71l3.114-.732a48.524 48.524 0 0 1-.005-10.499l-3.11.732a9 9 0 0 1-6.085-.711l-.108-.054a9 9 0 0 0-6.208-.682L3 4.5M3 15V4.5";
    private const string IconWarn    = "M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z";
    private const string IconSparkle = "M9.813 15.904 9 18.75l-.813-2.846a4.5 4.5 0 0 0-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 0 0 3.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 0 0 3.09 3.09L15.75 12l-2.847.813a4.5 4.5 0 0 0-3.09 3.09Z";
    private const string IconDoc     = "M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z";

    // Tone palettes (wrapper bg + icon color), aligned with the existing portal/admin styling.
    private const string ToneAccent  = "bg-accent/10";
    private const string ToneAccentI = "text-accent";
    private const string ToneEmerald = "bg-emerald-50 dark:bg-emerald-400/10";
    private const string ToneEmeraldI= "text-emerald-500 dark:text-emerald-400";
    private const string ToneAmber   = "bg-amber-50 dark:bg-amber-400/10";
    private const string ToneAmberI  = "text-amber-500 dark:text-amber-400";
    private const string ToneRose    = "bg-rose-50 dark:bg-rose-400/10";
    private const string ToneRoseI   = "text-rose-500 dark:text-rose-400";
    private const string ToneGray    = "bg-gray-100 dark:bg-white/5";
    private const string ToneGrayI   = "text-gray-500 dark:text-neutral-400";

    public static Visual Resolve(TimelineEventType type) => type switch
    {
        TimelineEventType.ProjectCreated     => new(IconSparkle, ToneAccent, ToneAccentI),
        TimelineEventType.StatusChanged      => new(IconBolt, ToneAccent, ToneAccentI),
        TimelineEventType.PhaseChanged       => new(IconFlag, ToneAccent, ToneAccentI),
        TimelineEventType.MilestoneCompleted => new(IconCheck, ToneEmerald, ToneEmeraldI),
        TimelineEventType.ProjectCompleted   => new(IconCheck, ToneEmerald, ToneEmeraldI),
        TimelineEventType.ProjectReopened    => new(IconBolt, ToneAmber, ToneAmberI),

        TimelineEventType.NoteAdded          => new(IconNote, ToneGray, ToneGrayI),

        TimelineEventType.InvoiceCreated     => new(IconInvoice, ToneAccent, ToneAccentI),
        TimelineEventType.InvoiceUpdated     => new(IconInvoice, ToneAccent, ToneAccentI),
        TimelineEventType.InvoiceSent        => new(IconInvoice, ToneAccent, ToneAccentI),
        TimelineEventType.InvoicePaid        => new(IconInvoice, ToneEmerald, ToneEmeraldI),
        TimelineEventType.InvoiceOverdue     => new(IconWarn, ToneRose, ToneRoseI),
        TimelineEventType.InvoiceViewed      => new(IconInvoice, ToneGray, ToneGrayI),

        TimelineEventType.FileUploaded       => new(IconDoc, ToneGray, ToneGrayI),
        TimelineEventType.DocumentShared     => new(IconDoc, ToneGray, ToneGrayI),
        TimelineEventType.TaskCreated        => new(IconFlag, ToneGray, ToneGrayI),
        TimelineEventType.TaskCompleted      => new(IconCheck, ToneEmerald, ToneEmeraldI),

        _                                    => new(IconBolt, ToneGray, ToneGrayI)
    };
}
