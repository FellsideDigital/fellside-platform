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

    /// <summary>
    /// Progress percentage (0–100) shown in the portal and admin. Derived from
    /// plan phases when they exist: a completed phase counts as 1, an in-progress
    /// phase as 0.5. Falls back to the manually-stored <see cref="Progress"/> value
    /// when there are no phases. This is the single source of truth — the raw
    /// <see cref="Progress"/> field is never written to by the app, so reading it
    /// directly always yields 0.
    /// </summary>
    public int ProgressPercent
    {
        get
        {
            if (PlanPhases.Count == 0) return Progress;
            var done = PlanPhases.Sum(p => p.Status switch
            {
                PhaseStatus.Completed => 1.0,
                PhaseStatus.InProgress => 0.5,
                _ => 0.0
            });
            return (int)Math.Round(done / PlanPhases.Count * 100);
        }
    }
}
