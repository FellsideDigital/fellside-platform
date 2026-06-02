using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

public interface IProjectTimelineService
{
    /// <summary>
    /// Append an event to a project's timeline. <paramref name="actorId"/> is resolved to a
    /// display-name snapshot internally. <paramref name="noteId"/> is set only for note events.
    /// </summary>
    Task RecordAsync(
        Guid projectId,
        TimelineEventType type,
        string summary,
        TimelineVisibility visibility = TimelineVisibility.ClientVisible,
        string? actorId = null,
        Guid? noteId = null,
        string? data = null,
        DateTime? occurredAt = null);

    /// <summary>
    /// Timeline events for a project, newest-first. When <paramref name="audience"/> is
    /// <see cref="TimelineAudience.Client"/>, internal-only events are excluded.
    /// </summary>
    Task<List<ProjectTimelineEvent>> GetForProjectAsync(Guid projectId, TimelineAudience audience);
}

public enum TimelineAudience
{
    Admin,
    Client
}
