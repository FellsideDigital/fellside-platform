using FellsideDigital.Domain.Enums;
using FellsideDigital.Domain.Extensions;
using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Services;

public class ProjectService(
    FellsideDigitalDbContext db,
    IStorageService storage,
    IProjectTimelineService timeline) : IProjectService
{
    private static DateTime? NormalizeToUtc(DateTime? value)
        => value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } dt => dt,
            { Kind: DateTimeKind.Local } dt => dt.ToUniversalTime(),
            { Kind: DateTimeKind.Unspecified } dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            var dt => dt
        };

    public async Task<ClientProject> CreateAsync(ClientProject project, string adminId)
    {
        project.CreatedByAdminId = adminId;
        project.TargetLaunchDate = NormalizeToUtc(project.TargetLaunchDate);
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        db.ClientProjects.Add(project);
        await db.SaveChangesAsync();

        await timeline.RecordAsync(
            project.Id, TimelineEventType.ProjectCreated, "Project created",
            TimelineVisibility.ClientVisible, actorId: adminId, occurredAt: project.CreatedAt);

        return project;
    }

    public async Task<ClientProject?> GetByIdAsync(Guid id)
        => await db.ClientProjects
            .Include(p => p.Client)
            .Include(p => p.Invoices)
            .Include(p => p.Notes.OrderByDescending(n => n.CreatedAt))
            .Include(p => p.TimelineEvents.OrderByDescending(e => e.OccurredAt))
                .ThenInclude(e => e.Note)
            .Include(p => p.PlanPhases.OrderBy(ph => ph.Order))
            .Include(p => p.Documents.OrderByDescending(d => d.CreatedAt))
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<ClientProject?> GetByIdForClientAsync(Guid id)
        => await db.ClientProjects
            .Include(p => p.Client)
            .Include(p => p.Invoices)
            .Include(p => p.TimelineEvents
                .Where(e => e.Visibility == TimelineVisibility.ClientVisible)
                .OrderByDescending(e => e.OccurredAt))
                .ThenInclude(e => e.Note)
            .Include(p => p.PlanPhases.OrderBy(ph => ph.Order))
            .Include(p => p.Documents.OrderByDescending(d => d.CreatedAt))
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<ClientProject>> GetAllAsync()
        => await db.ClientProjects
            .Include(p => p.Client)
            .Include(p => p.Invoices)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<int> GetProjectCountAsync()
        => await db.ClientProjects.CountAsync();

    public async Task<List<ClientProject>> GetForClientAsync(string clientId)
        => await db.ClientProjects
            .Include(p => p.Invoices)
            .Include(p => p.PlanPhases)
            .Include(p => p.TimelineEvents
                .Where(e => e.Visibility == TimelineVisibility.ClientVisible)
                .OrderByDescending(e => e.OccurredAt))
                .ThenInclude(e => e.Note)
            .Where(p => p.ClientId == clientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task UpdateAsync(ClientProject project, string? actorId = null)
    {
        // Read the persisted status before applying changes, so we can detect a transition.
        // AsNoTracking forces a DB read rather than returning the already-mutated tracked entity.
        var originalStatus = await db.ClientProjects
            .AsNoTracking()
            .Where(p => p.Id == project.Id)
            .Select(p => (ProjectStatus?)p.Status)
            .FirstOrDefaultAsync();

        project.TargetLaunchDate = NormalizeToUtc(project.TargetLaunchDate);
        project.UpdatedAt = DateTime.UtcNow;
        db.ClientProjects.Update(project);
        await db.SaveChangesAsync();

        if (originalStatus is { } from && from != project.Status)
            await RecordStatusChangeAsync(project.Id, from, project.Status, actorId);
    }

    private async Task RecordStatusChangeAsync(Guid projectId, ProjectStatus from, ProjectStatus to, string? actorId)
    {
        var (type, summary) = to switch
        {
            ProjectStatus.Completed =>
                (TimelineEventType.ProjectCompleted, "Project completed"),
            _ when from == ProjectStatus.Completed =>
                (TimelineEventType.ProjectReopened, $"Project reopened — now {to.DisplayName()}"),
            _ =>
                (TimelineEventType.StatusChanged, $"Status changed to {to.DisplayName()}")
        };

        await timeline.RecordAsync(projectId, type, summary, TimelineVisibility.ClientVisible, actorId);
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await db.ClientProjects
            .Include(p => p.Invoices)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return;

        // Remove associated blobs before the DB cascade drops the rows that point
        // at them, otherwise the invoice files / screenshot are orphaned in storage.
        // Best-effort: a missing or unreachable object must not block deletion.
        foreach (var invoice in project.Invoices)
        {
            if (string.IsNullOrWhiteSpace(invoice.FilePath)) continue;
            try { await storage.DeleteAsync(invoice.FilePath); }
            catch { /* non-fatal — proceed with deletion */ }
        }

        if (!string.IsNullOrWhiteSpace(project.ScreenshotPath))
        {
            try { await storage.DeleteAsync(project.ScreenshotPath); }
            catch { /* non-fatal */ }
        }

        db.ClientProjects.Remove(project);
        await db.SaveChangesAsync();
    }

    public async Task SavePhasesAsync(Guid projectId, List<ProjectPlanPhase> phases, string? actorId = null)
    {
        var existing = await db.ProjectPlanPhases
            .Where(ph => ph.ProjectId == projectId)
            .ToListAsync();

        // Phases are fully replaced on save and have no stable identity across edits, so we
        // diff old vs new by title to surface phase/milestone transitions on the timeline.
        var oldByTitle = existing
            .GroupBy(p => p.Title.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First().Status);

        db.ProjectPlanPhases.RemoveRange(existing);

        for (int i = 0; i < phases.Count; i++)
        {
            phases[i].Id = Guid.NewGuid();
            phases[i].ProjectId = projectId;
            phases[i].Order = i + 1;
            phases[i].TargetCompletionDate = NormalizeToUtc(phases[i].TargetCompletionDate);
            phases[i].CreatedAt = DateTime.UtcNow;
            phases[i].UpdatedAt = DateTime.UtcNow;
        }

        db.ProjectPlanPhases.AddRange(phases);
        await db.SaveChangesAsync();

        foreach (var phase in phases)
        {
            var key = phase.Title.Trim().ToLowerInvariant();
            if (!oldByTitle.TryGetValue(key, out var oldStatus) || oldStatus == phase.Status)
                continue;

            var (type, summary) = phase.Status == PhaseStatus.Completed
                ? (TimelineEventType.MilestoneCompleted, $"Milestone completed: {phase.Title}")
                : (TimelineEventType.PhaseChanged, $"\"{phase.Title}\" phase {phase.Status.DisplayName().ToLowerInvariant()}");

            await timeline.RecordAsync(projectId, type, summary, TimelineVisibility.ClientVisible, actorId);
        }
    }
}
