using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Web.Services;

public class ProjectDocumentService(
    FellsideDigitalDbContext db,
    IStorageService storage,
    IOptions<StorageSettings> storageOptions,
    IProjectTimelineService timeline) : IProjectDocumentService
{
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx"];

    private static readonly Dictionary<string, string> ContentTypes = new()
    {
        [".pdf"]  = "application/pdf",
        [".png"]  = "image/png",
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".doc"]  = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    public async Task<ProjectDocument> UploadAsync(Guid projectId, string title, IBrowserFile file, string? actorId = null)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed. Use PDF, Word, or an image.");

        var documentId = Guid.NewGuid();
        var key = $"documents/{projectId}/{documentId}{ext}";
        var contentType = ContentTypes.GetValueOrDefault(ext, "application/octet-stream");

        await using var stream = file.OpenReadStream(maxAllowedSize: 25 * 1024 * 1024);
        await storage.UploadAsync(key, stream, contentType);

        var document = new ProjectDocument
        {
            Id        = documentId,
            ProjectId = projectId,
            Title     = title,
            FilePath  = key,
            FileName  = file.Name,
            CreatedAt = DateTime.UtcNow,
        };

        db.ProjectDocuments.Add(document);
        await db.SaveChangesAsync();

        await timeline.RecordAsync(
            projectId, TimelineEventType.DocumentShared, $"Document shared: {title}",
            TimelineVisibility.ClientVisible, actorId, occurredAt: document.CreatedAt);

        return document;
    }

    public async Task<List<ProjectDocument>> GetForProjectAsync(Guid projectId)
        => await db.ProjectDocuments
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<string?> GetDownloadUrlAsync(Guid id)
    {
        var document = await db.ProjectDocuments.FindAsync(id);
        if (document is null || string.IsNullOrEmpty(document.FilePath)) return null;

        var expiry = TimeSpan.FromMinutes(storageOptions.Value.PresignedUrlExpiryMinutes);
        return await storage.GetPresignedUrlAsync(document.FilePath, expiry);
    }

    public async Task DeleteAsync(Guid id)
    {
        var document = await db.ProjectDocuments.FindAsync(id);
        if (document is null) return;

        if (!string.IsNullOrEmpty(document.FilePath))
        {
            try { await storage.DeleteAsync(document.FilePath); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"S3 delete failed for {document.FilePath}: {ex.Message}");
            }
        }

        db.ProjectDocuments.Remove(document);
        await db.SaveChangesAsync();
    }
}
