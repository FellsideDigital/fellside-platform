using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Services;

public class HeroProjectService(
    FellsideDigitalDbContext db,
    IStorageService storage) : IHeroProjectService
{
    private static readonly HashSet<string> AllowedImageExtensions = [".png", ".jpg", ".jpeg", ".webp"];

    private static readonly Dictionary<string, string> ContentTypes = new()
    {
        [".png"]  = "image/png",
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
    };

    private const long MaxScreenshotBytes = 8 * 1024 * 1024; // 8 MB

    public async Task<List<ClientProject>> GetHeroProjectsAsync()
    {
        var projects = await db.ClientProjects
            .AsNoTracking()
            .Where(p => p.IsHeroProject)
            .OrderBy(p => p.HeroDisplayOrder)
            .Include(p => p.Metrics.OrderBy(m => m.DisplayOrder))
            .Include(p => p.PipelineSteps.OrderBy(s => s.DisplayOrder))
            .Include(p => p.Integrations.OrderBy(i => i.DisplayOrder))
            .ToListAsync();

        // Resolve uploaded screenshots to displayable (presigned) URLs for the public page.
        foreach (var project in projects)
        {
            project.ScreenshotPath = await ResolveScreenshotUrlAsync(project.ScreenshotPath);
        }

        return projects;
    }

    public async Task<string> UploadScreenshotAsync(Guid projectId, IBrowserFile file)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed. Use a PNG, JPG, or WebP image.");

        var project = await db.ClientProjects.FindAsync(projectId)
            ?? throw new InvalidOperationException("Project not found.");

        var key = $"screenshots/{projectId}/{Guid.NewGuid()}{ext}";
        var contentType = ContentTypes.GetValueOrDefault(ext, "application/octet-stream");

        await using (var stream = file.OpenReadStream(maxAllowedSize: MaxScreenshotBytes))
        {
            await storage.UploadAsync(key, stream, contentType);
        }

        // Persist immediately (matches the invoice-upload pattern) and delete the
        // object it replaces, so the bucket never accumulates orphans.
        var previous = project.ScreenshotPath;
        project.ScreenshotPath = key;
        await db.SaveChangesAsync();
        await DeleteStorageObjectIfOwnedAsync(previous);

        return key;
    }

    public async Task RemoveScreenshotAsync(Guid projectId)
    {
        var project = await db.ClientProjects.FindAsync(projectId);
        if (project is null) return;

        var previous = project.ScreenshotPath;
        project.ScreenshotPath = null;
        await db.SaveChangesAsync();
        await DeleteStorageObjectIfOwnedAsync(previous);
    }

    // Deletes an object we uploaded. External URLs / rooted static paths aren't ours, so skip them.
    private async Task DeleteStorageObjectIfOwnedAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith('/'))
        {
            return;
        }

        try { await storage.DeleteAsync(path); }
        catch (Exception ex)
        {
            // Non-fatal: an orphaned object is recoverable and shouldn't block the edit.
            Console.Error.WriteLine($"Screenshot delete failed for {path}: {ex.Message}");
        }
    }

    public Task<string?> ResolveScreenshotUrlAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(path);

        // Absolute URLs (external screenshots) and rooted static paths are used as-is.
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith('/'))
        {
            return Task.FromResult<string?>(path);
        }

        // Otherwise it's a storage object key. Serve it through the app's own (HTTPS) origin
        // so it isn't blocked as mixed content when storage is an HTTP endpoint (dev MinIO),
        // and so storage stays private. See the "/media/{**key}" endpoint.
        return Task.FromResult<string?>("/media/" + path.TrimStart('/'));
    }

    public async Task SaveHeroSettingsAsync(Guid projectId, bool isHero, int displayOrder, string? tagline, string? showcaseUrl, string? screenshotPath)
    {
        var project = await db.ClientProjects.FindAsync(projectId);
        if (project is null) return;
        project.IsHeroProject = isHero;
        project.HeroDisplayOrder = displayOrder;
        project.HeroTagline = string.IsNullOrWhiteSpace(tagline) ? null : tagline.Trim();
        project.HeroShowcaseUrl = string.IsNullOrWhiteSpace(showcaseUrl) ? null : showcaseUrl.Trim();
        project.ScreenshotPath = string.IsNullOrWhiteSpace(screenshotPath) ? null : screenshotPath.Trim();
        await db.SaveChangesAsync();
    }

    public async Task SaveMetricsAsync(Guid projectId, List<ProjectMetric> metrics)
    {
        var existing = await db.ProjectMetrics.Where(m => m.ProjectId == projectId).ToListAsync();
        db.ProjectMetrics.RemoveRange(existing);
        var valid = metrics.Where(m => !string.IsNullOrWhiteSpace(m.Label) && !string.IsNullOrWhiteSpace(m.Value)).ToList();
        for (int i = 0; i < valid.Count; i++)
        {
            valid[i].Id = Guid.NewGuid();
            valid[i].ProjectId = projectId;
            valid[i].DisplayOrder = i;
        }
        db.ProjectMetrics.AddRange(valid);
        await db.SaveChangesAsync();
    }

    public async Task SavePipelineStepsAsync(Guid projectId, List<ProjectPipelineStep> steps)
    {
        var existing = await db.ProjectPipelineSteps.Where(s => s.ProjectId == projectId).ToListAsync();
        db.ProjectPipelineSteps.RemoveRange(existing);
        var valid = steps.Where(s => !string.IsNullOrWhiteSpace(s.Label)).ToList();
        for (int i = 0; i < valid.Count; i++)
        {
            valid[i].Id = Guid.NewGuid();
            valid[i].ProjectId = projectId;
            valid[i].DisplayOrder = i;
        }
        db.ProjectPipelineSteps.AddRange(valid);
        await db.SaveChangesAsync();
    }

    public async Task SaveIntegrationsAsync(Guid projectId, List<ProjectIntegration> integrations)
    {
        var existing = await db.ProjectIntegrations.Where(i => i.ProjectId == projectId).ToListAsync();
        db.ProjectIntegrations.RemoveRange(existing);
        var valid = integrations.Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();
        for (int i = 0; i < valid.Count; i++)
        {
            valid[i].Id = Guid.NewGuid();
            valid[i].ProjectId = projectId;
            valid[i].DisplayOrder = i;
        }
        db.ProjectIntegrations.AddRange(valid);
        await db.SaveChangesAsync();
    }
}
