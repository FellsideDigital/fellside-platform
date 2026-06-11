using System.ComponentModel.DataAnnotations;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Admin.Invitations;

public partial class Create : ComponentBase
{
    [Inject] private IInvitationService InvitationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<Create> Logger { get; set; } = default!;

    private InputModel Input { get; set; } = new();
    private string? _errorMessage;
    private string? _emailErrorDetails;
    private bool _submitting;

    private const string InputClass = FellsideDigital.UI.Components.Forms.FieldStyles.Input;

    private async Task CreateInvitationAsync()
    {
        _submitting = true;
        _errorMessage = null;
        _emailErrorDetails = null;

        try
        {
            var authState = await AuthState.GetAuthenticationStateAsync();
            var adminUser = await UserManager.GetUserAsync(authState.User);
            if (adminUser is null)
            {
                _errorMessage = "Could not determine the current admin user.";
                return;
            }

            var model = new ClientInvitation
            {
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                CompanyName = Input.CompanyName,
                JobTitle = Input.JobTitle,
                ServiceType = Input.ServiceType,
                ProjectDescription = Input.ProjectDescription,
                Notes = Input.Notes
            };

            var result = await InvitationService.CreateInvitationAsync(model, adminUser.Id);

            if (!string.IsNullOrEmpty(result.EmailError))
            {
                _errorMessage = "Invitation created but email failed to send.";
                _emailErrorDetails = result.EmailError;
                return;
            }

            NavigationManager.NavigateTo("/Admin/Invitations?success=1");
        }
        catch (Exception ex)
        {
            _errorMessage = ErrorHandling.LogAndDescribe(Logger, ex, "creating the invitation");
            _emailErrorDetails = null;
        }
        finally
        {
            _submitting = false;
        }
    }

    private sealed class InputModel
    {
        [Required] public string FirstName { get; set; } = "";
        [Required] public string LastName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string CompanyName { get; set; } = "";
        [Required] public string JobTitle { get; set; } = "";
        [Required] public string ServiceType { get; set; } = "";
        [Required] public string ProjectDescription { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}
