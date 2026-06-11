using System.ComponentModel.DataAnnotations;
using FellsideDigital.Domain.Enums;
using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Portal;

public partial class Testimonial : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private ITestimonialService Testimonials { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Testimonial> Logger { get; set; } = default!;

    private ApplicationUser? _user;
    private int _rating;
    private bool _loaded;
    private bool _saving;
    private string? _error;
    private TestimonialStatus? _currentStatus;

    private TestimonialModel Input { get; set; } = new();

    private const string InputClass = FellsideDigital.UI.Components.Forms.FieldStyles.Input;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        _user = await UserManager.GetUserAsync(authState.User);
        if (_user is null) { _loaded = true; return; }

        var existing = await Testimonials.GetForUserAsync(_user.Id);
        if (existing is not null)
        {
            _rating = existing.Rating;
            _currentStatus = existing.Status;
            Input.Quote = existing.Quote;
            Input.AuthorName = existing.AuthorName;
            Input.AuthorRole = existing.AuthorRole;
        }
        else
        {
            Input.AuthorName = $"{_user.FirstName} {_user.LastName}".Trim();
            Input.AuthorRole = ComposeRole(_user.JobTitle, _user.CompanyName);
        }

        _loaded = true;
    }

    private static string ComposeRole(string? jobTitle, string? company)
    {
        var parts = new[] { jobTitle, company }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());
        return string.Join(", ", parts);
    }

    private void SetRating(int value) => _rating = value;

    private async Task SubmitAsync()
    {
        if (_user is null) return;
        _saving = true;
        _error = null;

        try
        {
            await Testimonials.SubmitOrUpdateAsync(_user.Id, _rating, Input.Quote, Input.AuthorName, Input.AuthorRole);
            _currentStatus = TestimonialStatus.Pending;
            Toasts.Success("Thank you! Your testimonial has been submitted for review.");
        }
        catch (InvalidOperationException ex)
        {
            // Deliberate, user-facing validation message from the service.
            _error = ex.Message;
        }
        catch (Exception ex)
        {
            _error = ErrorHandling.LogAndDescribe(Logger, ex, "submitting your testimonial");
        }
        finally
        {
            _saving = false;
        }
    }

    private sealed class TestimonialModel
    {
        [Required(ErrorMessage = "Please write a few words for your testimonial.")]
        public string Quote { get; set; } = "";

        [Required(ErrorMessage = "Please tell us your name.")]
        public string AuthorName { get; set; } = "";

        public string AuthorRole { get; set; } = "";
    }
}
