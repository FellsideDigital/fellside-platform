using System.ComponentModel.DataAnnotations;
using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

// Editor view-models for the project Edit page, kept separate from the page's behaviour.
public partial class Edit
{
    private sealed class InputModel
    {
        [Required] public string Name { get; set; } = "";
        [Required] public string Description { get; set; } = "";
        public ProjectType Type { get; set; } = ProjectType.Website;
        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;
        public DateTime? TargetLaunchDate { get; set; }
        public string? PreviewUrl { get; set; }
        public string? ProjectUrl { get; set; }
        public string? DeploymentNotes { get; set; }
    }

    private sealed class PhaseEditorModel
    {
        public string Title { get; set; } = "";
        public string ShortLabel { get; set; } = "";
        public PhaseStatus Status { get; set; } = PhaseStatus.NotStarted;
        public DateTime? TargetCompletionDate { get; set; }
        public string? Notes { get; set; }
        public string? ImportantInformation { get; set; }
        public string? Dependencies { get; set; }
        public string? InternalNotes { get; set; }
        public bool IsExpanded { get; set; } = true;
    }

    private sealed class HeroInputModel
    {
        public bool IsHeroProject { get; set; }
        public int HeroDisplayOrder { get; set; }
        public string? HeroTagline { get; set; }
        public string? HeroShowcaseUrl { get; set; }
        public string? ScreenshotPath { get; set; }
    }

    private sealed class MetricEditorModel
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
        public MetricStyle Style { get; set; } = MetricStyle.Neutral;
    }

    private sealed class PipelineStepEditorModel
    {
        public string Label { get; set; } = "";
        public PipelineStepType StepType { get; set; } = PipelineStepType.Process;
    }

    private sealed class IntegrationEditorModel
    {
        public string Name { get; set; } = "";
    }
}
