using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

/// <summary>
/// An admin-authored project note (ServiceNow work-note style). Plain-text, multiline.
/// <see cref="Visibility"/> controls whether the client sees it in the portal timeline.
/// Editable by admins; read-only for clients. Each note has a matching
/// <see cref="ProjectTimelineEvent"/> of type <see cref="TimelineEventType.NoteAdded"/>.
/// </summary>
public class ProjectNote
{
    public Guid Id { get; set; }

    public string Body { get; set; } = "";
    public TimelineVisibility Visibility { get; set; } = TimelineVisibility.Internal;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }

    /// <summary>FK to the authoring admin. Nullable + SetNull so the note survives user deletion.</summary>
    public string? AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }

    /// <summary>Author display name snapshotted at write time, so it survives user deletion/rename.</summary>
    public string AuthorName { get; set; } = "";
}
