using System.Security.Claims;
using FellsideDigital.Domain.Enums;
using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Notes : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IProjectNoteService NoteService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Notes> Logger { get; set; } = default!;

    private ClientProject? _project;
    private List<ProjectNote> _notes = [];

    // Add-note form
    private string _newBody = "";
    private TimelineVisibility _newVisibility = TimelineVisibility.Internal;
    private bool _saving;

    // Inline edit state
    private Guid? _editingId;
    private string _editBody = "";
    private TimelineVisibility _editVisibility;

    private const string InputClass = FellsideDigital.UI.Components.Forms.FieldStyles.Input;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _project = await ProjectService.GetByIdAsync(Id);
        _notes = await NoteService.GetForProjectAsync(Id);
    }

    private async Task<string?> CurrentUserIdAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task AddNoteAsync()
    {
        if (string.IsNullOrWhiteSpace(_newBody)) return;
        var actorId = await CurrentUserIdAsync();
        if (actorId is null) return;

        _saving = true;
        try
        {
            await NoteService.AddAsync(Id, _newBody, _newVisibility, actorId);
            _newBody = "";
            _newVisibility = TimelineVisibility.Internal;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "adding the note"));
        }
        finally
        {
            _saving = false;
        }
    }

    private void StartEdit(ProjectNote note)
    {
        _editingId = note.Id;
        _editBody = note.Body;
        _editVisibility = note.Visibility;
    }

    private void CancelEdit()
    {
        _editingId = null;
        _editBody = "";
    }

    private async Task SaveEditAsync()
    {
        if (_editingId is not { } id || string.IsNullOrWhiteSpace(_editBody)) return;
        try
        {
            await NoteService.UpdateAsync(id, _editBody, _editVisibility);
            CancelEdit();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "updating the note"));
        }
    }

    private async Task DeleteNoteAsync(Guid noteId)
    {
        try
        {
            await NoteService.DeleteAsync(noteId);
            if (_editingId == noteId) CancelEdit();
            await LoadAsync();
            Toasts.Success("Note deleted.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "deleting the note"));
        }
    }
}
