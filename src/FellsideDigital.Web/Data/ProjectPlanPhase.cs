using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

public class ProjectPlanPhase
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public ClientProject Project { get; set; } = null!;

    public int Order { get; set; }

    public string Title { get; set; } = "";
    public string ShortLabel { get; set; } = "";
    public PhaseStatus Status { get; set; } = PhaseStatus.NotStarted;

    public DateTime? TargetCompletionDate { get; set; }
    public string? Notes { get; set; }
    public string? ImportantInformation { get; set; }
    public string? Dependencies { get; set; }
    public string? InternalNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
