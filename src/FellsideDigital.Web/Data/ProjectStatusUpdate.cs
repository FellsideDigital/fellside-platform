using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

public class ProjectStatusUpdate
{
    public Guid Id { get; set; }
    public string Message { get; set; } = "";
    public ProjectStatus? NewStatus { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }

    public string CreatedByAdminId { get; set; } = "";
    public ApplicationUser? CreatedByAdmin { get; set; }
}
