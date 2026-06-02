using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

public interface IProjectNoteService
{
    Task<ProjectNote> AddAsync(Guid projectId, string body, TimelineVisibility visibility, string authorId);
    Task UpdateAsync(Guid noteId, string body, TimelineVisibility visibility);
    Task DeleteAsync(Guid noteId);
    Task<List<ProjectNote>> GetForProjectAsync(Guid projectId);
}
