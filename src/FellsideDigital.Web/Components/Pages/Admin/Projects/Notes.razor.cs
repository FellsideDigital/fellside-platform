using System.Security.Claims;
using FellsideDigital.Domain.Enums;
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

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-accent transition-shadow outline-none";

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
        await NoteService.AddAsync(Id, _newBody, _newVisibility, actorId);
        _newBody = "";
        _newVisibility = TimelineVisibility.Internal;
        _saving = false;
        await LoadAsync();
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
        await NoteService.UpdateAsync(id, _editBody, _editVisibility);
        CancelEdit();
        await LoadAsync();
    }

    private async Task DeleteNoteAsync(Guid noteId)
    {
        await NoteService.DeleteAsync(noteId);
        if (_editingId == noteId) CancelEdit();
        await LoadAsync();
    }
}
