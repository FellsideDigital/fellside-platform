using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components.Forms;

namespace FellsideDigital.Web.Services;

public interface IHeroProjectService
{
    Task<List<ClientProject>> GetHeroProjectsAsync();
    Task SaveHeroSettingsAsync(Guid projectId, bool isHero, int displayOrder, string? tagline, string? showcaseUrl, string? screenshotPath);
    Task SaveMetricsAsync(Guid projectId, List<ProjectMetric> metrics);
    Task SavePipelineStepsAsync(Guid projectId, List<ProjectPipelineStep> steps);
    Task SaveIntegrationsAsync(Guid projectId, List<ProjectIntegration> integrations);

    /// <summary>
    /// Uploads a screenshot image to storage, sets it as the project's ScreenshotPath and saves
    /// immediately, deleting the object it replaces. Returns the new object key.
    /// </summary>
    Task<string> UploadScreenshotAsync(Guid projectId, IBrowserFile file);

    /// <summary>Clears the project's screenshot and deletes the stored object (if we own it).</summary>
    Task RemoveScreenshotAsync(Guid projectId);

    /// <summary>
    /// Turns a stored ScreenshotPath into a browser-displayable URL. Absolute URLs and rooted
    /// static paths pass through unchanged; storage object keys are resolved to presigned URLs.
    /// </summary>
    Task<string?> ResolveScreenshotUrlAsync(string? path);
}
