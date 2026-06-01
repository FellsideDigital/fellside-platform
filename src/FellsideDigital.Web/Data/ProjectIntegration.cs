namespace FellsideDigital.Web.Data;

public class ProjectIntegration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public ClientProject Project { get; set; } = null!;

    public string Name { get; set; } = "";
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
