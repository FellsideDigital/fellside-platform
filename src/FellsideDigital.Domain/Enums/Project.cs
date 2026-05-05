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