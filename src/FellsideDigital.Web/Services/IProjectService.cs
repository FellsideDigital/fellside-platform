using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

public interface IProjectService
{
    Task<ClientProject> CreateAsync(ClientProject project, string adminId);
    Task<ClientProject?> GetByIdAsync(Guid id);
    Task<List<ClientProject>> GetAllAsync();
    Task<List<ClientProject>> GetForClientAsync(string clientId);
    Task UpdateAsync(ClientProject project);
    Task DeleteAsync(Guid id);
    Task AddStatusUpdateAsync(Guid projectId, string message, ProjectStatus? newStatus, string adminId);
    Task<List<ProjectStatusUpdate>> GetStatusUpdatesAsync(Guid projectId);
}
