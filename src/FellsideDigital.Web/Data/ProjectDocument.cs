namespace FellsideDigital.Web.Data;

public class ProjectDocument
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }

    public string Title { get; set; } = "";
    public string FilePath { get; set; } = "";  // S3 object key — not a web URL
    public string FileName { get; set; } = "";  // original filename for display

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
