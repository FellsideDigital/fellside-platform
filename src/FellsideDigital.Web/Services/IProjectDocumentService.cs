using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components.Forms;

namespace FellsideDigital.Web.Services;

public interface IProjectDocumentService
{
    Task<ProjectDocument> UploadAsync(Guid projectId, string title, IBrowserFile file, string? actorId = null);
    Task<List<ProjectDocument>> GetForProjectAsync(Guid projectId);
    Task DeleteAsync(Guid id);

    /// <summary>Time-limited presigned download URL for the document file, or null if missing.</summary>
    Task<string?> GetDownloadUrlAsync(Guid id);
}
