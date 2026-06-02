using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Services;

public class ProjectNoteService(FellsideDigitalDbContext db, IProjectTimelineService timeline) : IProjectNoteService
{
    public async Task<ProjectNote> AddAsync(Guid projectId, string body, TimelineVisibility visibility, string authorId)
    {
        var note = new ProjectNote
        {
            Id         = Guid.NewGuid(),
            ProjectId  = projectId,
            Body       = body.Trim(),
            Visibility = visibility,
            AuthorId   = authorId,
            AuthorName = await ResolveUserNameAsync(authorId) ?? "",
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow
        };

        db.ProjectNotes.Add(note);
        await db.SaveChangesAsync();

        // Mirror the note into the timeline. The note body is rendered live via the NoteId
        // join, so the summary is just a label; visibility tracks the note's visibility.
        await timeline.RecordAsync(
            projectId, TimelineEventType.NoteAdded, "Note added",
            visibility, actorId: authorId, noteId: note.Id, occurredAt: note.CreatedAt);

        return note;
    }

    public async Task UpdateAsync(Guid noteId, string body, TimelineVisibility visibility)
    {
        var note = await db.ProjectNotes.FindAsync(noteId);
        if (note is null) return;

        note.Body       = body.Trim();
        note.Visibility = visibility;
        note.UpdatedAt  = DateTime.UtcNow;

        // Keep the matching timeline event's visibility in sync so a note flipped to
        // internal disappears from (and client-visible reappears in) the client feed.
        var events = await db.ProjectTimelineEvents.Where(e => e.NoteId == noteId).ToListAsync();
        foreach (var e in events) e.Visibility = visibility;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid noteId)
    {
        // Remove the timeline event(s) first so the client never sees a dangling entry,
        // then the note itself.
        var events = await db.ProjectTimelineEvents.Where(e => e.NoteId == noteId).ToListAsync();
        db.ProjectTimelineEvents.RemoveRange(events);

        var note = await db.ProjectNotes.FindAsync(noteId);
        if (note is not null) db.ProjectNotes.Remove(note);

        await db.SaveChangesAsync();
    }

    public async Task<List<ProjectNote>> GetForProjectAsync(Guid projectId)
        => await db.ProjectNotes
            .Where(n => n.ProjectId == projectId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

    private async Task<string?> ResolveUserNameAsync(string userId)
    {
        var u = await db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.FirstName, x.LastName, x.CompanyName, x.Email })
            .FirstOrDefaultAsync();

        if (u is null) return null;

        var full = $"{u.FirstName} {u.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(full)) return full;
        if (!string.IsNullOrWhiteSpace(u.CompanyName)) return u.CompanyName;
        return u.Email;
    }
}
