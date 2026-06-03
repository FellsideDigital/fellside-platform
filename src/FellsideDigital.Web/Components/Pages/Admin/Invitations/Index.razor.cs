using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FellsideDigital.Web.Components.Pages.Admin.Invitations;

public partial class Index : ComponentBase
{
    [Inject] private IInvitationService InvitationService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Index> Logger { get; set; } = default!;

    private List<ClientInvitation>? _invitations;
    private Guid? _openActionsFor;
    private Guid? _resendingId;

    [SupplyParameterFromQuery]
    private string? Success { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _invitations = await InvitationService.GetValidInvitationsAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && Success == "1")
            Toasts.Success("Invitation created and sent successfully.");
    }

    private async Task CopyLink(ClientInvitation inv)
    {
        _openActionsFor = null;

        var url = NavigationManager.ToAbsoluteUri(
            $"/Account/Register?token={Uri.EscapeDataString(inv.Token)}").ToString();
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", url);
            Toasts.Success($"Invitation link for {inv.FirstName} copied to clipboard.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "copying the invitation link"));
        }
    }

    private void ToggleActions(Guid invitationId)
    {
        _openActionsFor = _openActionsFor == invitationId ? null : invitationId;
    }

    private async Task ResendAsync(ClientInvitation inv)
    {
        _resendingId = inv.Id;
        _openActionsFor = null;
        try
        {
            var emailError = await InvitationService.ResendInvitationAsync(inv.Id);
            if (emailError is null)
            {
                Toasts.Success($"Invitation resent to {inv.Email}.");
                _invitations = await InvitationService.GetValidInvitationsAsync();
            }
            else
            {
                Toasts.Error($"Could not resend invitation: {emailError}");
            }
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "resending the invitation"));
        }
        finally
        {
            _resendingId = null;
        }
    }

    private async Task RevokeAsync(Guid id)
    {
        _openActionsFor = null;
        try
        {
            await InvitationService.RevokeInvitationAsync(id);
            _invitations = await InvitationService.GetValidInvitationsAsync();
            Toasts.Success("Invitation revoked.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "revoking the invitation"));
        }
    }
}
