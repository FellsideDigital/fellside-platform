using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

public class ProjectPipelineStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public ClientProject Project { get; set; } = null!;

    public PipelineStepType StepType { get; set; } = PipelineStepType.Process;
    public string Label { get; set; } = "";
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
