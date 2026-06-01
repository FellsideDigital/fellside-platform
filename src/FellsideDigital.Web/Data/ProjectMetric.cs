using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

public class ProjectMetric
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public ClientProject Project { get; set; } = null!;

    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public MetricStyle Style { get; set; } = MetricStyle.Neutral;
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
