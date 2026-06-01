using System.ComponentModel.DataAnnotations;

namespace FellsideDigital.Domain.Enums;

public enum ProjectStatus
{
    Pending,
    [Display(Name = "In Progress")] InProgress,
    Blocked,
    [Display(Name = "On Hold")] OnHold,
    Completed
}

public enum ProjectType
{
    Website,
    Automation
}

public enum PhaseStatus
{
    [Display(Name = "Not Started")] NotStarted,
    [Display(Name = "In Progress")] InProgress,
    Blocked,
    [Display(Name = "On Hold")] OnHold,
    Completed
}

public enum MetricStyle
{
    Neutral,
    Up,
    Speed,
    Warm
}

public enum PipelineStepType
{
    Trigger,
    Process,
    Output
}