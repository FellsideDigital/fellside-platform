using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

/// <summary>
/// A single entry in a project's timeline — the materialized, append-only record of
/// everything that happens on a project (notes, status/phase changes, invoice lifecycle,
/// …). <see cref="Summary"/> and <see cref="ActorName"/> are snapshotted at write time so
/// the entry stays accurate and auditable even if the underlying entity or user is later
/// edited or deleted. Note events additionally keep a live <see cref="NoteId"/> join so an
/// edited note body stays current in the feed.
/// </summary>
public class ProjectTimelineEvent
{
    public Guid Id { get; set; }

    public TimelineEventType Type { get; set; }

    /// <summary>User-friendly description, frozen at write time (e.g. "Invoice #1024 paid").</summary>
    public string Summary { get; set; } = "";

    public TimelineVisibility Visibility { get; set; } = TimelineVisibility.ClientVisible;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }

    /// <summary>The admin/user who triggered the event. Nullable + SetNull → event survives user deletion.</summary>
    public string? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }

    /// <summary>Actor display name snapshotted at write time. Empty for system-generated events.</summary>
    public string? ActorName { get; set; }

    /// <summary>Set only for <see cref="TimelineEventType.NoteAdded"/> — links to the live note body.</summary>
    public Guid? NoteId { get; set; }
    public ProjectNote? Note { get; set; }

    /// <summary>Optional JSON payload for extra structured detail future event types may need.</summary>
    public string? Data { get; set; }
}
