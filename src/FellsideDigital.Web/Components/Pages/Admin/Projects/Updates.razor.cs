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

    private const string InputClass = FellsideDigital.UI.Components.Forms.FieldStyles.Input;

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
