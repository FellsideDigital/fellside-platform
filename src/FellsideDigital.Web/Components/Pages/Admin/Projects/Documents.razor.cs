using System.Security.Claims;
using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Documents : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IProjectDocumentService DocumentService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Documents> Logger { get; set; } = default!;

    private ClientProject? _project;
    private List<ProjectDocument> _documents = [];
    private Dictionary<Guid, string> _downloadUrls = [];

    private string _newTitle = "";
    private IBrowserFile? _selectedFile;
    private bool _saving;
    private string? _error;

    private const string InputClass = FellsideDigital.UI.Components.Forms.FieldStyles.Input;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _project = await ProjectService.GetByIdAsync(Id);
        _documents = await DocumentService.GetForProjectAsync(Id);

        _downloadUrls = [];
        foreach (var doc in _documents)
        {
            try { _downloadUrls[doc.Id] = await DocumentService.GetDownloadUrlAsync(doc.Id) ?? ""; }
            catch { /* non-fatal */ }
        }
    }

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        _selectedFile = e.File;
        if (string.IsNullOrWhiteSpace(_newTitle))
            _newTitle = Path.GetFileNameWithoutExtension(e.File.Name);
    }

    private async Task<string?> CurrentUserIdAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task UploadAsync()
    {
        _error = null;
        if (string.IsNullOrWhiteSpace(_newTitle) || _selectedFile is null) return;

        _saving = true;
        try
        {
            var actorId = await CurrentUserIdAsync();
            await DocumentService.UploadAsync(Id, _newTitle.Trim(), _selectedFile, actorId);
            _newTitle = "";
            _selectedFile = null;
        }
        catch (InvalidOperationException ex)
        {
            // Deliberate, user-facing validation message from the service.
            _error = ex.Message;
        }
        catch (Exception ex)
        {
            _error = ErrorHandling.LogAndDescribe(Logger, ex, "uploading the document");
        }
        finally
        {
            _saving = false;
        }
        await LoadAsync();
    }

    private async Task DeleteAsync(Guid documentId)
    {
        try
        {
            await DocumentService.DeleteAsync(documentId);
            await LoadAsync();
            Toasts.Success("Document deleted.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "deleting the document"));
        }
    }
}
