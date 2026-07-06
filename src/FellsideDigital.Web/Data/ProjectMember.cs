using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

/// <summary>Join row linking an additional person (collaborator) to a project.
/// The primary client is stored separately on <see cref="ClientProject.ClientId"/>.</summary>
public class ProjectMember
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }

    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Collaborator;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
