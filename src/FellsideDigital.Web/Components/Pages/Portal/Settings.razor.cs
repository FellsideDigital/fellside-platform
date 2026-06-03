using System.ComponentModel.DataAnnotations;
using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Portal;

public partial class Settings : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Settings> Logger { get; set; } = default!;

    private ApplicationUser? _user;
    private string _email = "";

    private ProfileModel ProfileInput { get; set; } = new();
    private PasswordModel PasswordInput { get; set; } = new();

    private bool _savingProfile;
    private bool _changingPassword;
    private string? _profileError;
    private string? _passwordError;

    private const string InputClass = FellsideDigital.UI.Components.Forms.FieldStyles.Input;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        _user = await UserManager.GetUserAsync(authState.User);
        if (_user is null) return;

        _email = _user.Email ?? "";
        ProfileInput.FirstName = _user.FirstName ?? "";
        ProfileInput.LastName = _user.LastName ?? "";
    }

    private async Task SaveProfileAsync()
    {
        if (_user is null) return;
        _savingProfile = true;
        _profileError = null;

        _user.FirstName = ProfileInput.FirstName;
        _user.LastName = ProfileInput.LastName;

        try
        {
            var result = await UserManager.UpdateAsync(_user);
            if (result.Succeeded)
                Toasts.Success("Profile updated successfully.");
            else
                _profileError = string.Join(" ", result.Errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            _profileError = ErrorHandling.LogAndDescribe(Logger, ex, "updating your profile");
        }
        finally
        {
            _savingProfile = false;
        }
    }

    private async Task ChangePasswordAsync()
    {
        if (_user is null) return;
        if (PasswordInput.NewPassword != PasswordInput.ConfirmPassword)
        {
            _passwordError = "New passwords do not match.";
            return;
        }

        _changingPassword = true;
        _passwordError = null;

        try
        {
            var result = await UserManager.ChangePasswordAsync(_user, PasswordInput.CurrentPassword, PasswordInput.NewPassword);
            if (result.Succeeded)
            {
                Toasts.Success("Password updated successfully.");
                PasswordInput = new();
            }
            else
            {
                _passwordError = string.Join(" ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            _passwordError = ErrorHandling.LogAndDescribe(Logger, ex, "changing your password");
        }
        finally
        {
            _changingPassword = false;
        }
    }

    private sealed class ProfileModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    private sealed class PasswordModel
    {
        [Required] public string CurrentPassword { get; set; } = "";
        [Required, MinLength(8)] public string NewPassword { get; set; } = "";
        [Required] public string ConfirmPassword { get; set; } = "";
    }
}
