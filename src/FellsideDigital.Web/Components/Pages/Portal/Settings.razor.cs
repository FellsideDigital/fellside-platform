using System.ComponentModel.DataAnnotations;
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

    private ApplicationUser? _user;
    private string _email = "";

    private ProfileModel ProfileInput { get; set; } = new();
    private PasswordModel PasswordInput { get; set; } = new();

    private bool _savingProfile;
    private bool _changingPassword;
    private string? _profileSuccess;
    private string? _profileError;
    private string? _passwordSuccess;
    private string? _passwordError;

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-indigo-400 dark:focus:ring-orange-400 transition-shadow outline-none";

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
        _profileSuccess = null;
        _profileError = null;

        _user.FirstName = ProfileInput.FirstName;
        _user.LastName = ProfileInput.LastName;

        var result = await UserManager.UpdateAsync(_user);
        _savingProfile = false;

        if (result.Succeeded)
            _profileSuccess = "Profile updated successfully.";
        else
            _profileError = string.Join(" ", result.Errors.Select(e => e.Description));
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
        _passwordSuccess = null;
        _passwordError = null;

        var result = await UserManager.ChangePasswordAsync(_user, PasswordInput.CurrentPassword, PasswordInput.NewPassword);
        _changingPassword = false;

        if (result.Succeeded)
        {
            _passwordSuccess = "Password updated successfully.";
            PasswordInput = new();
        }
        else
        {
            _passwordError = string.Join(" ", result.Errors.Select(e => e.Description));
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
