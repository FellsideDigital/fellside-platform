using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Services;

public class ProjectService(FellsideDigitalDbContext db, IStorageService storage) : IProjectService
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
        return project;
    }

    public async Task<ClientProject?> GetByIdAsync(Guid id)
        => await db.ClientProjects
            .Include(p => p.Client)
            .Include(p => p.Invoices)
            .Include(p => p.StatusUpdates.OrderByDescending(u => u.CreatedAt))
                .ThenInclude(u => u.CreatedByAdmin)
            .Include(p => p.PlanPhases.OrderBy(ph => ph.Order))
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<ClientProject>> GetAllAsync()
        => await db.ClientProjects
            .Include(p => p.Client)
            .Include(p => p.Invoices)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<List<ClientProject>> GetForClientAsync(string clientId)
        => await db.ClientProjects
            .Include(p => p.Invoices)
            .Include(p => p.PlanPhases)
            .Include(p => p.StatusUpdates.OrderByDescending(u => u.CreatedAt))
                .ThenInclude(u => u.CreatedByAdmin)
            .Where(p => p.ClientId == clientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task UpdateAsync(ClientProject project)
    {
        project.TargetLaunchDate = NormalizeToUtc(project.TargetLaunchDate);
        project.UpdatedAt = DateTime.UtcNow;
        db.ClientProjects.Update(project);
        await db.SaveChangesAsync();
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

    public async Task AddStatusUpdateAsync(Guid projectId, string message, ProjectStatus? newStatus, string adminId)
    {
        var update = new ProjectStatusUpdate
        {
            ProjectId = projectId,
            Message = message,
            NewStatus = newStatus,
            CreatedByAdminId = adminId,
            CreatedAt = DateTime.UtcNow
        };
        db.ProjectStatusUpdates.Add(update);

        if (newStatus.HasValue)
        {
            var project = await db.ClientProjects.FindAsync(projectId);
            if (project is not null)
            {
                project.Status = newStatus.Value;
                project.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task<List<ProjectStatusUpdate>> GetStatusUpdatesAsync(Guid projectId)
        => await db.ProjectStatusUpdates
            .Include(u => u.CreatedByAdmin)
            .Where(u => u.ProjectId == projectId)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

    public async Task SavePhasesAsync(Guid projectId, List<ProjectPlanPhase> phases)
    {
        var existing = await db.ProjectPlanPhases
            .Where(ph => ph.ProjectId == projectId)
            .ToListAsync();

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
    }
}
