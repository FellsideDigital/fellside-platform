using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Services;

public class ProjectTimelineService(FellsideDigitalDbContext db) : IProjectTimelineService
{
    public async Task RecordAsync(
        Guid projectId,
        TimelineEventType type,
        string summary,
        TimelineVisibility visibility = TimelineVisibility.ClientVisible,
        string? actorId = null,
        Guid? noteId = null,
        string? data = null,
        DateTime? occurredAt = null)
    {
        db.ProjectTimelineEvents.Add(new ProjectTimelineEvent
        {
            Id         = Guid.NewGuid(),
            ProjectId  = projectId,
            Type       = type,
            Summary    = summary,
            Visibility = visibility,
            ActorId    = actorId,
            ActorName  = await ResolveActorNameAsync(actorId),
            NoteId     = noteId,
            Data       = data,
            OccurredAt = occurredAt ?? DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    public async Task<List<ProjectTimelineEvent>> GetForProjectAsync(Guid projectId, TimelineAudience audience)
    {
        var query = db.ProjectTimelineEvents
            .Include(e => e.Note)
            .Where(e => e.ProjectId == projectId);

        if (audience == TimelineAudience.Client)
            query = query.Where(e => e.Visibility == TimelineVisibility.ClientVisible);

        return await query.OrderByDescending(e => e.OccurredAt).ToListAsync();
    }

    /// <summary>
    /// Best-effort display name for an actor, snapshotted onto the event. Returns null for
    /// system-generated events (no actor) or if the user can't be found.
    /// </summary>
    private async Task<string?> ResolveActorNameAsync(string? actorId)
    {
        if (string.IsNullOrWhiteSpace(actorId)) return null;

        var u = await db.Users
            .AsNoTracking()
            .Where(x => x.Id == actorId)
            .Select(x => new { x.FirstName, x.LastName, x.CompanyName, x.Email })
            .FirstOrDefaultAsync();

        if (u is null) return null;

        var full = $"{u.FirstName} {u.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(full)) return full;
        if (!string.IsNullOrWhiteSpace(u.CompanyName)) return u.CompanyName;
        return u.Email;
    }
}
