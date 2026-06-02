using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Updates : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    private ClientProject? _project;
    private string _updateMessage = "";
    private string _newStatus = "";
    private bool _postingUpdate;

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-accent transition-shadow outline-none";

    protected override async Task OnInitializedAsync() => _project = await ProjectService.GetByIdAsync(Id);

    private async Task PostUpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(_updateMessage)) return;
        _postingUpdate = true;
        var authState = await AuthState.GetAuthenticationStateAsync();
        var admin = await UserManager.GetUserAsync(authState.User);
        ProjectStatus? status = Enum.TryParse<ProjectStatus>(_newStatus, out var parsed) ? parsed : null;
        await ProjectService.AddStatusUpdateAsync(Id, _updateMessage.Trim(), status, admin!.Id);
        _updateMessage = "";
        _newStatus = "";
        _postingUpdate = false;
        _project = await ProjectService.GetByIdAsync(Id);
    }
}
