namespace FellsideDigital.Web.Services;

/// <summary>
/// Holds the optional "preview as client" state for the current Blazor circuit.
/// When an admin launches a portal preview, the portal data pages resolve the
/// client whose data to show through <see cref="ResolveClientId"/> instead of
/// always using the logged-in user. Scoped to the circuit: it survives in-app
/// navigation but a hard browser refresh clears it (dropping back to the admin's
/// own account).
/// </summary>
public class PortalPreviewState
{
    public string? PreviewClientId { get; private set; }
    public string? PreviewClientName { get; private set; }
    public Guid? SourceProjectId { get; private set; }

    public bool IsActive => !string.IsNullOrEmpty(PreviewClientId);

    public void Enter(string clientId, string clientName, Guid sourceProjectId)
    {
        PreviewClientId = clientId;
        PreviewClientName = clientName;
        SourceProjectId = sourceProjectId;
    }

    public void Exit()
    {
        PreviewClientId = null;
        PreviewClientName = null;
        SourceProjectId = null;
    }

    /// <summary>
    /// Returns the client id whose data should be shown. The preview override is
    /// only honoured for site admins with an active preview; everyone else (and
    /// admins not previewing) gets their own id back.
    /// </summary>
    public string ResolveClientId(string currentUserId, bool isSiteAdmin)
        => isSiteAdmin && IsActive ? PreviewClientId! : currentUserId;
}
