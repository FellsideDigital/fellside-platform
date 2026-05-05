using System.Security.Claims;
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Portal;

public partial class Index : ComponentBase
{
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private bool _loading = true;
    private string _firstName = "there";
    private string _companyName = "your";
    private string _greeting = "Hello";
    private string? _userId;

    private List<ClientProject> _projects = [];
    private List<ClientProject> _websites = [];
    private List<ClientProject> _automations = [];
    private List<Invoice> _invoices = [];
    private List<Invoice> _overdueInvoices = [];
    private List<(ProjectStatusUpdate Update, ClientProject Project)> _recentActivity = [];

    private string _bookingsUrl = "";

    protected override async Task OnInitializedAsync()
    {
        _bookingsUrl = Configuration["SupportBookingsUrl"] ?? "";

        _greeting = DateTime.Now.Hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _    => "Good evening",
        };

        var authState = await AuthState.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(_userId)) { _loading = false; return; }

        var user = await UserManager.GetUserAsync(authState.User);
        if (user is not null)
        {
            _firstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName
                : authState.User.FindFirstValue(ClaimTypes.Email) ?? "there";
            _companyName = !string.IsNullOrWhiteSpace(user.CompanyName) ? user.CompanyName : "your";
        }

        _projects    = await ProjectService.GetForClientAsync(_userId);
        _invoices    = await InvoiceService.GetForClientAsync(_userId);
        _websites    = _projects.Where(p => p.Type == ProjectType.Website).ToList();
        _automations = _projects.Where(p => p.Type == ProjectType.Automation).ToList();
        _overdueInvoices = _invoices.Where(i => i.Status == InvoiceStatus.Overdue).ToList();

        _recentActivity = _projects
            .SelectMany(p => p.StatusUpdates.Select(u => (Update: u, Project: p)))
            .OrderByDescending(x => x.Update.CreatedAt)
            .Take(6)
            .ToList();

        _loading = false;
    }
}
