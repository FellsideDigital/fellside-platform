using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

public interface IProjectService
{
    Task<ClientProject> CreateAsync(ClientProject project, string adminId);
    Task<ClientProject?> GetByIdAsync(Guid id);
    Task<ClientProject?> GetByIdForClientAsync(Guid id);
    Task<List<ClientProject>> GetAllAsync();
    Task<List<ClientProject>> GetForClientAsync(string clientId);
    Task UpdateAsync(ClientProject project, string? actorId = null);
    Task DeleteAsync(Guid id);
    Task SavePhasesAsync(Guid projectId, List<ProjectPlanPhase> phases, string? actorId = null);
}
