using System.ComponentModel.DataAnnotations;
using FellsideDigital.Domain.Enums;
using FellsideDigital.Domain.Extensions;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace FellsideDigital.Web.Components.Pages;

public partial class Contact : ComponentBase
{
    [Inject] private FellsideDigitalDbContext Db { get; set; } = default!;
    [Inject] private EmailService EmailService { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private string _bookingsUrl = "";

    protected override void OnInitialized()
    {
        _bookingsUrl = Configuration["BookingsUrl"] ?? "";
    }

    private readonly string[] _steps = ["Your details", "About you", "Your message"];
    private int _currentStep;

    private void NextStep() { if (_currentStep < _steps.Length - 1) _currentStep++; }
    private void PreviousStep() { if (_currentStep > 0) _currentStep--; }
    private void TryGoToStep(int index) { if (index <= _currentStep) _currentStep = index; }

    private ContactFormModel _model = new();
    private bool _sending;
    private bool _submitted;

    private async Task HandleSubmit()
    {
        _sending = true;
        StateHasChanged();

        var enquiry = new ContactEnquiry
        {
            Id = Guid.NewGuid(),
            Name = _model.Name,
            Email = _model.Email,
            Phone = _model.Phone,
            Company = _model.Company,
            ServiceType = _model.ServiceType!.Value.DisplayName(),
            Budget = _model.Budget?.DisplayName(),
            Message = _model.Message,
            HowHeard = _model.HowHeard?.DisplayName(),
            SubmittedAt = DateTimeOffset.UtcNow
        };


        Db.ContactEnquiries.Add(enquiry);
        await Db.SaveChangesAsync();

        try { await EmailService.SendContactEnquiryAsync(enquiry); }
        catch { /* saved to DB regardless — email failure is non-fatal */ }

        _sending = false;
        _submitted = true;
    }

    private const string InputClass =
        "w-full rounded-xl px-4 py-2.5 text-sm " +
        "bg-slate-50 dark:bg-neutral-800 " +
        "ring-1 ring-slate-200 dark:ring-white/10 " +
        "text-slate-900 dark:text-neutral-100 " +
        "placeholder:text-slate-400 dark:placeholder:text-neutral-500 " +
        "focus:outline-none focus:ring-2 focus:ring-indigo-400/50 dark:focus:ring-orange-400/50 transition";

    private sealed class ContactFormModel
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Company { get; set; }

        [Required(ErrorMessage = "Please select a service.")]
        public ContactServiceType? ServiceType { get; set; }

        public ContactBudget? Budget { get; set; }

        [Required(ErrorMessage = "Please tell us about your project.")]
        [MinLength(10, ErrorMessage = "Please give us a bit more detail (min 10 characters).")]
        public string Message { get; set; } = string.Empty;

        public ContactHowHeard? HowHeard { get; set; }
    }
}
