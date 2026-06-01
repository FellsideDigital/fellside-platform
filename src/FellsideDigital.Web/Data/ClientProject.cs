using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

public class ClientProject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ProjectStatus Status { get; set; } = ProjectStatus.Pending;
    public ProjectType Type { get; set; }

    public string? PreviewUrl { get; set; }
    public string? ProjectUrl { get; set; }
    public string? DeploymentNotes { get; set; }

    public int Progress { get; set; } = 0;
    public DateTime? TargetLaunchDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string ClientId { get; set; } = "";
    public ApplicationUser? Client { get; set; }

    public string CreatedByAdminId { get; set; } = "";
    public ApplicationUser? CreatedByAdmin { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<ProjectStatusUpdate> StatusUpdates { get; set; } = [];
    public ICollection<ProjectPlanPhase> PlanPhases { get; set; } = [];

    // Hero showcase
    public bool IsHeroProject { get; set; } = false;
    public int HeroDisplayOrder { get; set; } = 0;
    public string? HeroTagline { get; set; }
    public string? HeroShowcaseUrl { get; set; }
    public string? ScreenshotPath { get; set; }

    public ICollection<ProjectMetric> Metrics { get; set; } = [];
    public ICollection<ProjectPipelineStep> PipelineSteps { get; set; } = [];
    public ICollection<ProjectIntegration> Integrations { get; set; } = [];
}
