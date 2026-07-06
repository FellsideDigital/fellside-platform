using FellsideDigital.Domain.Enums;
using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class PeopleCard : ComponentBase
{
    [Parameter, EditorRequired] public ClientProject Project { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IInvitationService InvitationService { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<PeopleCard> Logger { get; set; } = default!;

    private List<ApplicationUser> _clients = [];
    private string _selectedPrimaryId = "";
    private string _selectedMemberId = "";

    private bool _showInvite;
    private bool _inviteAsPrimary;
    private bool _sendingInvite;
    private string? _inviteError;
    private string _inviteFirstName = "";
    private string _inviteLastName = "";
    private string _inviteEmail = "";

    protected override async Task OnInitializedAsync()
    {
        var all = UserManager.Users.ToList();
        var adminIds = (await UserManager.GetUsersInRoleAsync("SiteAdmin")).Select(u => u.Id).ToHashSet();
        _clients = all.Where(u => !adminIds.Contains(u.Id)).ToList();
    }

    private IEnumerable<ApplicationUser> AssignableUsers()
    {
        var onProject = new HashSet<string>(Project.Members.Select(m => m.UserId));
        if (Project.ClientId is not null) onProject.Add(Project.ClientId);
        return _clients.Where(u => !onProject.Contains(u.Id));
    }

    private static string DisplayName(ApplicationUser? u)
    {
        if (u is null) return "Unknown";
        var full = $"{u.FirstName} {u.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(full)) return full;
        if (!string.IsNullOrWhiteSpace(u.CompanyName)) return u.CompanyName!;
        return u.Email ?? "Unknown";
    }

    private async Task<string?> AdminIdAsync()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        return (await UserManager.GetUserAsync(state.User))?.Id;
    }

    private async Task SetPrimaryAsync()
    {
        if (string.IsNullOrEmpty(_selectedPrimaryId)) return;
        await MutateAsync(async adminId =>
        {
            await ProjectService.SetPrimaryClientAsync(Project.Id, _selectedPrimaryId, adminId);
            _selectedPrimaryId = "";
        }, "setting the primary client", "Primary client set.");
    }

    private async Task ClearPrimaryAsync() =>
        await MutateAsync(async adminId =>
            await ProjectService.SetPrimaryClientAsync(Project.Id, null, adminId),
            "clearing the primary client", "Primary client cleared.");

    private async Task AddMemberAsync()
    {
        if (string.IsNullOrEmpty(_selectedMemberId)) return;
        await MutateAsync(async adminId =>
        {
            await ProjectService.AddMemberAsync(Project.Id, _selectedMemberId, adminId);
            _selectedMemberId = "";
        }, "adding the collaborator", "Collaborator added.");
    }

    private async Task RemoveMemberAsync(string userId) =>
        await MutateAsync(async _ =>
            await ProjectService.RemoveMemberAsync(Project.Id, userId),
            "removing the collaborator", "Collaborator removed.");

    private async Task MutateAsync(Func<string, Task> action, string doing, string success)
    {
        try
        {
            var adminId = await AdminIdAsync();
            if (adminId is null) { Toasts.Error("Could not identify admin user."); return; }
            await action(adminId);
            Toasts.Success(success);
            await OnChanged.InvokeAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, doing));
        }
    }

    private void OpenInvite(bool asPrimary)
    {
        _inviteAsPrimary = asPrimary;
        _inviteError = null;
        _inviteFirstName = _inviteLastName = _inviteEmail = "";
        _showInvite = true;
    }

    private async Task SendInviteAsync()
    {
        _inviteError = null;
        if (string.IsNullOrWhiteSpace(_inviteEmail) || string.IsNullOrWhiteSpace(_inviteFirstName))
        {
            _inviteError = "First name and email are required.";
            return;
        }

        _sendingInvite = true;
        try
        {
            var adminId = await AdminIdAsync();
            if (adminId is null) { _inviteError = "Could not identify admin user."; return; }

            var invitation = new ClientInvitation
            {
                Email = _inviteEmail.Trim(),
                FirstName = _inviteFirstName.Trim(),
                LastName = _inviteLastName.Trim(),
                ProjectId = Project.Id,
                IsPrimaryClient = _inviteAsPrimary,
            };

            var (_, emailError) = await InvitationService.CreateInvitationAsync(invitation, adminId);
            _showInvite = false;
            if (emailError is null)
                Toasts.Success($"Invitation sent to {invitation.Email}.");
            else
                Toasts.Error("Invitation created, but the email failed to send.");

            await OnChanged.InvokeAsync();
        }
        catch (Exception ex)
        {
            _inviteError = ErrorHandling.LogAndDescribe(Logger, ex, "sending the invitation");
        }
        finally
        {
            _sendingInvite = false;
        }
    }
}
