using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin;

public partial class Dashboard : ComponentBase
{
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private IRecurringInvoiceService RecurringService { get; set; } = default!;
    [Inject] private IEnquiryService EnquiryService { get; set; } = default!;
    [Inject] private IInvitationService InvitationService { get; set; } = default!;

    private bool _loading = true;

    private List<ClientProject> _projects = [];
    private List<Invoice> _invoices = [];
    private List<RecurringInvoiceSchedule> _schedules = [];
    private List<ContactEnquiry> _enquiries = [];
    private List<ClientInvitation> _invitations = [];

    // ── KPI figures ──
    private int _activeProjects => _projects.Count(p => p.Status == ProjectStatus.InProgress);

    private decimal _outstandingTotal => _invoices
        .Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue)
        .Sum(i => i.Amount);

    private int _overdueCount => _invoices.Count(i => i.Status == InvoiceStatus.Overdue);
    private int _unreadEnquiries => _enquiries.Count(e => !e.IsRead);
    private int _pendingInvitations => _invitations.Count(i => i.Status == InvitationStatus.Pending);

    private decimal _totalEarned => InvoiceEarnings.TotalEarned(_invoices, _schedules, DateTime.UtcNow);

    // ── Lists for the panels ──
    private IEnumerable<ClientProject> _recentProjects =>
        _projects.OrderByDescending(p => p.UpdatedAt).Take(5);

    private IEnumerable<Invoice> _overdueInvoices =>
        _invoices.Where(i => i.Status == InvoiceStatus.Overdue)
                 .OrderBy(i => i.DueAt)
                 .Take(5);

    protected override async Task OnInitializedAsync()
    {
        _projects = await ProjectService.GetAllAsync();
        _invoices = await InvoiceService.GetAllAsync();
        _schedules = await RecurringService.GetAllSchedulesAsync();
        _enquiries = await EnquiryService.GetAllAsync();
        _invitations = await InvitationService.GetAllInvitationsAsync();
        _loading = false;
    }

    /// <summary>Display name for a client: "First Last · Company", falling back to email, then "Unassigned".</summary>
    private static string ClientName(ApplicationUser? client)
    {
        if (client is null) return "Unassigned";
        var name = $"{client.FirstName} {client.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(name)) name = client.Email ?? "Client";
        return string.IsNullOrWhiteSpace(client.CompanyName) ? name : $"{name} · {client.CompanyName}";
    }
}
