using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Services;

public class ProjectService(FellsideDigitalDbContext db) : IProjectService
{
    public async Task<ClientProject> CreateAsync(ClientProject project, string adminId)
    {
        project.CreatedByAdminId = adminId;
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
            .Include(p => p.StatusUpdates.OrderByDescending(u => u.CreatedAt))
                .ThenInclude(u => u.CreatedByAdmin)
            .Where(p => p.ClientId == clientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task UpdateAsync(ClientProject project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        db.ClientProjects.Update(project);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await db.ClientProjects.FindAsync(id);
        if (project is not null)
        {
            db.ClientProjects.Remove(project);
            await db.SaveChangesAsync();
        }
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
}
