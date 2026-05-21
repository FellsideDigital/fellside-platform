# QR Code Campaign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add two trackable QR codes (t-shirt + business card) that log every scan, land visitors on a compelling offer page, capture leads via a form, and display analytics in the admin panel.

**Architecture:** Two minimal API endpoints (`/q/shirt`, `/q/card`) write a `QrScan` row then redirect to a public Blazor page `/scan`. The landing page presents an exclusive discount offer and captures `QrLead` records. An admin page at `/Admin/QrCampaign` shows scan counts by source and a leads table. Discount code `LAUNCH26` is hardcoded on the landing page — Oliver applies it manually when quoting (no coupon engine needed).

**Tech Stack:** ASP.NET Core Minimal API (redirect endpoints), Blazor Interactive Server, EF Core + PostgreSQL, Tailwind CSS

---

## QR Code URLs (generate these externally)

| Medium        | Scan URL                              |
|---------------|---------------------------------------|
| T-shirt       | `https://fellsidedigital.co.uk/q/shirt` |
| Business card | `https://fellsidedigital.co.uk/q/card`  |

Generate QR codes pointing at these URLs via any QR generator (e.g. qr.io, QR Tiger). The short paths keep QR density low = easier to scan.

---

## File Map

**New:**
- `src/FellsideDigital.Web/Data/QrScan.cs` — scan log entity
- `src/FellsideDigital.Web/Data/QrLead.cs` — lead capture entity
- `src/FellsideDigital.Web/Components/Pages/Marketing/Scan.razor` — public landing page
- `src/FellsideDigital.Web/Components/Pages/Marketing/Scan.razor.cs` — landing page code-behind
- `src/FellsideDigital.Web/Components/Pages/Admin/QrCampaign/Index.razor` — admin dashboard
- `src/FellsideDigital.Web/Components/Pages/Admin/QrCampaign/Index.razor.cs` — admin dashboard code-behind

**Modified:**
- `src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs` — add DbSets + FK config for QrLead→QrScan
- `src/FellsideDigital.Web/Extensions/StartupCompositionExtensions.cs` — add `MapQrRedirects()` call + method

**Generated (do not hand-write):**
- `src/FellsideDigital.Web/Data/Migrations/<timestamp>_AddQrCampaign.cs` — via `dotnet ef migrations add`

---

## Task 1: Data Entities

**Files:**
- Create: `src/FellsideDigital.Web/Data/QrScan.cs`
- Create: `src/FellsideDigital.Web/Data/QrLead.cs`

- [ ] **Step 1: Create `QrScan.cs`**

```csharp
namespace FellsideDigital.Web.Data;

public class QrScan
{
    public Guid Id { get; set; }
    public string Source { get; set; } = "";   // "shirt" | "card"
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public ICollection<QrLead> Leads { get; set; } = [];
}
```

- [ ] **Step 2: Create `QrLead.cs`**

```csharp
namespace FellsideDigital.Web.Data;

public class QrLead
{
    public Guid Id { get; set; }
    public string Source { get; set; } = "";   // "shirt" | "card"
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string Interest { get; set; } = ""; // "Website design" | "Automation" | "Both" | "Not sure yet"
    public string? Message { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public Guid? QrScanId { get; set; }
    public QrScan? QrScan { get; set; }
}
```

- [ ] **Step 3: Commit**

```
git add src/FellsideDigital.Web/Data/QrScan.cs src/FellsideDigital.Web/Data/QrLead.cs
git commit -m "feat: add QrScan and QrLead entities"
```

---

## Task 2: DbContext + EF Migration

**Files:**
- Modify: `src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs`

- [ ] **Step 1: Add DbSets** (after the `ContactEnquiries` line)

```csharp
public DbSet<QrScan> QrScans => Set<QrScan>();
public DbSet<QrLead> QrLeads => Set<QrLead>();
```

- [ ] **Step 2: Add FK config in `OnModelCreating`** (after the `ProjectPlanPhase` block, before the closing `}`)

```csharp
builder.Entity<QrLead>(e =>
{
    e.HasOne(l => l.QrScan)
        .WithMany(s => s.Leads)
        .HasForeignKey(l => l.QrScanId)
        .IsRequired(false)
        .OnDelete(DeleteBehavior.SetNull);
});
```

- [ ] **Step 3: Run migration**

```
dotnet ef migrations add AddQrCampaign --project src/FellsideDigital.Web
```

Expected: new file at `src/FellsideDigital.Web/Data/Migrations/<timestamp>_AddQrCampaign.cs` creating `QrScans` and `QrLeads` tables.

- [ ] **Step 4: Commit**

```
git add src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs
git add src/FellsideDigital.Web/Data/Migrations/
git commit -m "feat: add QrScan and QrLead to DbContext with migration"
```

---

## Task 3: QR Redirect Endpoints

**Files:**
- Modify: `src/FellsideDigital.Web/Extensions/StartupCompositionExtensions.cs`

The endpoints use Minimal API: log the scan, then 302-redirect to `/scan?from={source}&ref={scanId}`. The `ref` param lets the landing page link the eventual lead back to this specific scan.

- [ ] **Step 1: Add using** at the top of `StartupCompositionExtensions.cs` (if not already present)

```csharp
using FellsideDigital.Web.Data;
```

- [ ] **Step 2: Call `MapQrRedirects()` inside `UseFellsideDigitalPlatform`** — add it after `app.UseAntiforgery()` and before `app.MapStaticAssets()`:

```csharp
app.MapQrRedirects();
```

- [ ] **Step 3: Add the private method** at the bottom of `StartupCompositionExtensions.cs` (inside the class, after `UseFellsideDigitalPlatform`):

```csharp
private static void MapQrRedirects(this WebApplication app)
{
    var validSources = new HashSet<string> { "shirt", "card" };

    app.MapGet("/q/{source}", async (string source, FellsideDigitalDbContext db, HttpContext ctx) =>
    {
        var normalized = validSources.Contains(source.ToLower()) ? source.ToLower() : "unknown";

        var scan = new QrScan
        {
            Source    = normalized,
            IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
            UserAgent = ctx.Request.Headers.UserAgent.ToString(),
        };

        db.QrScans.Add(scan);
        await db.SaveChangesAsync();

        return Results.Redirect($"/scan?from={normalized}&ref={scan.Id}");
    });
}
```

- [ ] **Step 4: Verify**

Start the app (`dotnet run --launch-profile http`) and hit `http://localhost:5185/q/shirt`. Confirm:
- Browser lands on `/scan?from=shirt&ref=<some-guid>`
- A row exists in the `QrScans` table with `Source = "shirt"`

- [ ] **Step 5: Commit**

```
git add src/FellsideDigital.Web/Extensions/StartupCompositionExtensions.cs
git commit -m "feat: add QR scan logging redirect endpoints /q/shirt and /q/card"
```

---

## Task 4: Public Landing Page (`/scan`)

**Files:**
- Create: `src/FellsideDigital.Web/Components/Pages/Marketing/Scan.razor`
- Create: `src/FellsideDigital.Web/Components/Pages/Marketing/Scan.razor.cs`

This is a **public** page (no `[Authorize]`). It reads `?from=` to personalise the headline and `?ref=` to link the lead back to the scan. After submit it shows the discount code `LAUNCH26`.

> **To change the discount code or offer %:** search `LAUNCH26` and `15%` in `Scan.razor` — both appear once each.

- [ ] **Step 1: Create `Scan.razor`**

```razor
@page "/scan"
@using FellsideDigital.Web.Components.Pages.Marketing
@layout MainLayout

<PageTitle>Exclusive Offer — Fellside Digital</PageTitle>

<div class="min-h-screen bg-gray-950 text-white flex flex-col">

    <!-- Hero -->
    <section class="flex flex-col items-center justify-center text-center px-6 pt-24 pb-16">
        <div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-accent/10 border border-accent/20 text-accent text-sm font-medium mb-6">
            <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9.813 15.904 9 18.75l-.813-2.846a4.5 4.5 0 0 0-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 0 0 3.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 0 0 3.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 0 0-3.09 3.09Z" />
            </svg>
            @SourceLabel
        </div>

        <h1 class="text-4xl sm:text-5xl font-bold tracking-tight max-w-2xl">
            Nice to meet you.
        </h1>
        <p class="mt-4 text-lg text-neutral-400 max-w-xl">
            As a thank you for scanning, here's an exclusive offer — fill in your details below to claim it.
        </p>
    </section>

    <!-- Offer cards -->
    <section class="flex justify-center px-6 pb-16">
        <div class="w-full max-w-2xl rounded-2xl border border-white/10 bg-white/5 p-8 flex flex-col sm:flex-row gap-8">
            <div class="flex-1 border-b sm:border-b-0 sm:border-r border-white/10 pb-6 sm:pb-0 sm:pr-8">
                <p class="text-xs font-semibold uppercase tracking-widest text-neutral-500 mb-2">What you get</p>
                <p class="text-2xl font-bold text-white">15% off</p>
                <p class="text-neutral-400 mt-1 text-sm">your first project — applied directly to your quote</p>
            </div>
            <div class="flex-1">
                <p class="text-xs font-semibold uppercase tracking-widest text-neutral-500 mb-2">Plus</p>
                <p class="text-2xl font-bold text-white">Free discovery call</p>
                <p class="text-neutral-400 mt-1 text-sm">30 minutes, no obligation, no sales pitch</p>
            </div>
        </div>
    </section>

    <!-- Form / success -->
    <section class="flex justify-center px-6 pb-24">
        <div class="w-full max-w-xl">

            @if (!_submitted)
            {
                <div class="rounded-2xl border border-white/10 bg-white/5 p-8">
                    <h2 class="text-lg font-semibold text-white mb-6">Claim your offer</h2>

                    <div class="space-y-4">

                        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                            <div>
                                <label class="block text-xs font-semibold uppercase tracking-widest text-neutral-500 mb-1.5">Name *</label>
                                <input @bind="_name" type="text" placeholder="Your name"
                                       class="w-full rounded-xl bg-white/5 border border-white/10 px-4 py-2.5 text-sm text-white placeholder:text-neutral-600 focus:outline-none focus:ring-2 focus:ring-accent/50" />
                            </div>
                            <div>
                                <label class="block text-xs font-semibold uppercase tracking-widest text-neutral-500 mb-1.5">Email *</label>
                                <input @bind="_email" type="email" placeholder="your@email.com"
                                       class="w-full rounded-xl bg-white/5 border border-white/10 px-4 py-2.5 text-sm text-white placeholder:text-neutral-600 focus:outline-none focus:ring-2 focus:ring-accent/50" />
                            </div>
                        </div>

                        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                            <div>
                                <label class="block text-xs font-semibold uppercase tracking-widest text-neutral-500 mb-1.5">Phone</label>
                                <input @bind="_phone" type="tel" placeholder="Optional"
                                       class="w-full rounded-xl bg-white/5 border border-white/10 px-4 py-2.5 text-sm text-white placeholder:text-neutral-600 focus:outline-none focus:ring-2 focus:ring-accent/50" />
                            </div>
                            <div>
                                <label class="block text-xs font-semibold uppercase tracking-widest text-neutral-500 mb-1.5">Company</label>
                                <input @bind="_company" type="text" placeholder="Optional"
                                       class="w-full rounded-xl bg-white/5 border border-white/10 px-4 py-2.5 text-sm text-white placeholder:text-neutral-600 focus:outline-none focus:ring-2 focus:ring-accent/50" />
                            </div>
                        </div>

                        <div>
                            <label class="block text-xs font-semibold uppercase tracking-widest text-neutral-500 mb-1.5">I'm interested in *</label>
                            <div class="grid grid-cols-2 gap-2">
                                @foreach (var option in _interestOptions)
                                {
                                    <button type="button"
                                            @onclick="() => _interest = option"
                                            class="@(_interest == option
                                                ? "bg-accent/20 border-accent text-white"
                                                : "bg-white/5 border-white/10 text-neutral-400 hover:border-white/20")
                                                   rounded-xl border px-4 py-2.5 text-sm text-left transition-colors">
                                        @option
                                    </button>
                                }
                            </div>
                        </div>

                        <div>
                            <label class="block text-xs font-semibold uppercase tracking-widest text-neutral-500 mb-1.5">Anything else?</label>
                            <textarea @bind="_message" rows="3"
                                      placeholder="Tell me a bit about what you're looking for..."
                                      class="w-full rounded-xl bg-white/5 border border-white/10 px-4 py-2.5 text-sm text-white placeholder:text-neutral-600 focus:outline-none focus:ring-2 focus:ring-accent/50 resize-none"></textarea>
                        </div>

                        @if (!string.IsNullOrEmpty(_error))
                        {
                            <p class="text-sm text-red-400">@_error</p>
                        }

                        <button type="button" @onclick="SubmitAsync" disabled="@_saving"
                                class="w-full rounded-xl bg-accent px-6 py-3 text-sm font-semibold text-white
                                       hover:opacity-90 transition-opacity disabled:opacity-50">
                            @(_saving ? "Saving..." : "Claim my offer →")
                        </button>
                    </div>
                </div>
            }
            else
            {
                <div class="rounded-2xl border border-green-500/20 bg-green-500/5 p-8 text-center">
                    <div class="flex justify-center mb-4">
                        <div class="size-12 rounded-full bg-green-500/10 flex items-center justify-center">
                            <svg class="size-6 text-green-400" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                            </svg>
                        </div>
                    </div>

                    <h2 class="text-xl font-bold text-white mb-2">You're all set!</h2>
                    <p class="text-neutral-400 text-sm mb-8">Use this code when you get in touch — it'll be applied to your first quote.</p>

                    <div class="rounded-xl border border-white/10 bg-white/5 px-8 py-5 mb-8 inline-block">
                        <p class="text-xs font-semibold uppercase tracking-widest text-neutral-500 mb-1">Your discount code</p>
                        <p class="text-3xl font-bold tracking-widest text-accent">LAUNCH26</p>
                        <p class="text-xs text-neutral-500 mt-1">Valid for 60 days</p>
                    </div>

                    <div class="text-left space-y-3">
                        <p class="text-xs font-semibold uppercase tracking-widest text-neutral-500 mb-1">What happens next</p>
                        <div class="flex items-start gap-3">
                            <span class="size-5 shrink-0 rounded-full bg-accent/10 text-accent text-xs flex items-center justify-center font-bold mt-0.5">1</span>
                            <p class="text-sm text-neutral-300">I'll be in touch within 24 hours to say hello.</p>
                        </div>
                        <div class="flex items-start gap-3">
                            <span class="size-5 shrink-0 rounded-full bg-accent/10 text-accent text-xs flex items-center justify-center font-bold mt-0.5">2</span>
                            <p class="text-sm text-neutral-300">We'll book a free 30-minute discovery call at a time that suits you.</p>
                        </div>
                        <div class="flex items-start gap-3">
                            <span class="size-5 shrink-0 rounded-full bg-accent/10 text-accent text-xs flex items-center justify-center font-bold mt-0.5">3</span>
                            <p class="text-sm text-neutral-300">When we put together your quote, the 15% discount is applied automatically.</p>
                        </div>
                    </div>
                </div>
            }

        </div>
    </section>

</div>
```

- [ ] **Step 2: Create `Scan.razor.cs`**

```csharp
using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Marketing;

public partial class Scan : ComponentBase
{
    [Inject] private FellsideDigitalDbContext Db { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "from")] public string? From { get; set; }
    [SupplyParameterFromQuery(Name = "ref")]  public string? Ref  { get; set; }

    private string SourceLabel => From switch
    {
        "shirt" => "You scanned my t-shirt",
        "card"  => "You scanned my business card",
        _       => "You found Fellside Digital",
    };

    private readonly string[] _interestOptions = ["Website design", "Automation", "Both", "Not sure yet"];

    private string _name     = "";
    private string _email    = "";
    private string _phone    = "";
    private string _company  = "";
    private string _interest = "";
    private string _message  = "";
    private string _error    = "";
    private bool   _saving;
    private bool   _submitted;

    private async Task SubmitAsync()
    {
        _error = "";

        if (string.IsNullOrWhiteSpace(_name))     { _error = "Please enter your name.";          return; }
        if (string.IsNullOrWhiteSpace(_email))    { _error = "Please enter your email.";         return; }
        if (string.IsNullOrWhiteSpace(_interest)) { _error = "Please select what you need help with."; return; }

        _saving = true;

        var lead = new QrLead
        {
            Source   = From ?? "unknown",
            Name     = _name.Trim(),
            Email    = _email.Trim(),
            Phone    = string.IsNullOrWhiteSpace(_phone)   ? null : _phone.Trim(),
            Company  = string.IsNullOrWhiteSpace(_company) ? null : _company.Trim(),
            Interest = _interest,
            Message  = string.IsNullOrWhiteSpace(_message) ? null : _message.Trim(),
            QrScanId = Guid.TryParse(Ref, out var scanId) ? scanId : null,
        };

        Db.QrLeads.Add(lead);
        await Db.SaveChangesAsync();

        _submitted = true;
        _saving    = false;
    }
}
```

- [ ] **Step 3: Verify**

Start the app and hit `http://localhost:5185/scan?from=shirt&ref=00000000-0000-0000-0000-000000000000`:
- Badge shows "You scanned my t-shirt"
- Offer cards render (15% off + free discovery call)
- Form validates: submitting without name/email/interest shows error message
- Filling all required fields and submitting shows success state with `LAUNCH26`
- Check DB: a `QrLeads` row exists with correct `Source`, `Name`, `Email`, `Interest`

Hit `/scan?from=card` — badge changes to "You scanned my business card".
Hit `/scan` (no params) — badge shows "You found Fellside Digital".

- [ ] **Step 4: Commit**

```
git add src/FellsideDigital.Web/Components/Pages/Marketing/Scan.razor
git add src/FellsideDigital.Web/Components/Pages/Marketing/Scan.razor.cs
git commit -m "feat: add public QR landing page with offer and lead capture"
```

---

## Task 5: Admin QR Campaign Dashboard

**Files:**
- Create: `src/FellsideDigital.Web/Components/Pages/Admin/QrCampaign/Index.razor`
- Create: `src/FellsideDigital.Web/Components/Pages/Admin/QrCampaign/Index.razor.cs`

Follows the same pattern as `/Admin/Enquiries`: stats cards at the top, a table of leads, a side drawer for detail + mark-read.

- [ ] **Step 1: Create `Index.razor`**

```razor
@page "/Admin/QrCampaign"
@attribute [Authorize(Roles = "SiteAdmin")]
@layout AdminLayout

@using Microsoft.AspNetCore.Authorization
@using FellsideDigital.Web.Data
@using FellsideDigital.Web.Components.Layout
@using FellsideDigital.UI.Layout

<PageTitle>QR Campaign — Fellside Digital Admin</PageTitle>

<PageHeader Eyebrow="Admin" Title="QR Campaign" Description="Scan analytics and leads from business cards and t-shirts." />

<!-- Stats -->
@if (_stats is null)
{
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-8">
        @for (int i = 0; i < 4; i++)
        {
            <div class="animate-pulse h-20 rounded-xl bg-gray-100 dark:bg-white/5"></div>
        }
    </div>
}
else
{
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-8">
        <div class="rounded-xl border border-gray-200 dark:border-white/5 bg-white dark:bg-neutral-900 p-5">
            <p class="text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500">Total Scans</p>
            <p class="mt-1.5 text-3xl font-bold text-gray-900 dark:text-white">@_stats.TotalScans</p>
        </div>
        <div class="rounded-xl border border-gray-200 dark:border-white/5 bg-white dark:bg-neutral-900 p-5">
            <p class="text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500">T-Shirt</p>
            <p class="mt-1.5 text-3xl font-bold text-gray-900 dark:text-white">@_stats.ShirtScans</p>
        </div>
        <div class="rounded-xl border border-gray-200 dark:border-white/5 bg-white dark:bg-neutral-900 p-5">
            <p class="text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500">Business Card</p>
            <p class="mt-1.5 text-3xl font-bold text-gray-900 dark:text-white">@_stats.CardScans</p>
        </div>
        <div class="rounded-xl border border-gray-200 dark:border-white/5 bg-white dark:bg-neutral-900 p-5">
            <p class="text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500">Leads</p>
            <p class="mt-1.5 text-3xl font-bold text-gray-900 dark:text-white">@_stats.TotalLeads</p>
        </div>
    </div>
}

<!-- Leads table -->
@if (_leads is null)
{
    <div class="animate-pulse space-y-3">
        @for (int i = 0; i < 3; i++)
        {
            <div class="rounded-xl border border-gray-200 dark:border-white/5 bg-white dark:bg-neutral-900 h-16"></div>
        }
    </div>
}
else if (_leads.Count == 0)
{
    <EmptyState Padding="p-14"
                IconContainerClass="size-12 rounded-2xl mb-4"
                Title="No leads yet"
                Subtitle="When someone fills in the form after scanning, they'll appear here.">
        <Icon>
            <svg class="size-6 text-gray-400 dark:text-neutral-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 4.875c0-.621.504-1.125 1.125-1.125h4.5c.621 0 1.125.504 1.125 1.125v4.5c0 .621-.504 1.125-1.125 1.125h-4.5A1.125 1.125 0 0 1 3.75 9.375v-4.5ZM3.75 14.625c0-.621.504-1.125 1.125-1.125h4.5c.621 0 1.125.504 1.125 1.125v4.5c0 .621-.504 1.125-1.125 1.125h-4.5a1.125 1.125 0 0 1-1.125-1.125v-4.5ZM13.5 4.875c0-.621.504-1.125 1.125-1.125h4.5c.621 0 1.125.504 1.125 1.125v4.5c0 .621-.504 1.125-1.125 1.125h-4.5A1.125 1.125 0 0 1 13.5 9.375v-4.5Z" />
            </svg>
        </Icon>
    </EmptyState>
}
else
{
    <div class="rounded-2xl border border-gray-200 dark:border-white/5 overflow-hidden bg-white dark:bg-neutral-900">

        <div class="grid grid-cols-[auto_1fr_1fr_1fr_auto_auto] gap-4 px-5 py-3
                    border-b border-gray-100 dark:border-white/5
                    text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500">
            <span>Source</span>
            <span>Name</span>
            <span>Interest</span>
            <span>Company</span>
            <span>Date</span>
            <span></span>
        </div>

        @foreach (var lead in _leads)
        {
            <div class="@(lead.IsRead ? "" : "bg-accent-hover/40")
                        grid grid-cols-[auto_1fr_1fr_1fr_auto_auto] gap-4 items-center
                        px-5 py-4 border-b border-gray-100 dark:border-white/5 last:border-0
                        hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">

                <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium
                             @(lead.Source == "shirt"
                                ? "bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300"
                                : "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300")">
                    @(lead.Source == "shirt" ? "T-shirt" : "Card")
                </span>

                <div class="min-w-0">
                    <div class="flex items-center gap-2">
                        @if (!lead.IsRead)
                        {
                            <span class="size-1.5 rounded-full bg-accent shrink-0"></span>
                        }
                        <p class="text-sm font-semibold text-gray-900 dark:text-white truncate">@lead.Name</p>
                    </div>
                    <p class="text-xs text-gray-500 dark:text-neutral-400 truncate mt-0.5">@lead.Email</p>
                </div>

                <p class="text-sm text-gray-700 dark:text-neutral-300 truncate">@lead.Interest</p>
                <p class="text-sm text-gray-500 dark:text-neutral-400 truncate">@(lead.Company ?? "—")</p>

                <p class="text-xs text-gray-400 dark:text-neutral-500 whitespace-nowrap">
                    @lead.SubmittedAt.ToString("d MMM yyyy")
                </p>

                <button type="button" @onclick="() => OpenLead(lead)"
                        class="text-xs font-medium text-accent-hover0 hover:underline underline-offset-2 whitespace-nowrap">
                    View
                </button>
            </div>
        }
    </div>
}

<!-- Detail drawer -->
@if (_selected is not null)
{
    <div class="fixed inset-0 z-40 flex justify-end" @onclick="CloseDrawer">
        <div class="absolute inset-0 bg-black/30 dark:bg-black/50 backdrop-blur-sm"></div>

        <div class="relative z-50 w-full max-w-lg bg-white dark:bg-neutral-900
                    shadow-2xl flex flex-col h-full overflow-y-auto"
             @onclick:stopPropagation>

            <div class="flex items-center justify-between px-6 py-5 border-b border-gray-100 dark:border-white/5">
                <div>
                    <p class="text-base font-semibold text-gray-900 dark:text-white">@_selected.Name</p>
                    <a href="mailto:@_selected.Email" class="text-sm text-accent-hover0 hover:underline">@_selected.Email</a>
                </div>
                <button type="button" @onclick="CloseDrawer"
                        class="rounded-lg p-1.5 text-gray-400 hover:bg-gray-100 dark:hover:bg-white/10 transition-colors">
                    <svg class="size-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
                    </svg>
                </button>
            </div>

            <div class="px-6 py-5 space-y-5 flex-1">
                <dl class="grid grid-cols-2 gap-x-6 gap-y-4">
                    <div>
                        <dt class="text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500">Source</dt>
                        <dd class="mt-1 text-sm text-gray-900 dark:text-white">@(_selected.Source == "shirt" ? "T-Shirt" : "Business Card")</dd>
                    </div>
                    <div>
                        <dt class="text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500">Interest</dt>
                        <dd class="mt-1 text-sm text-gray-900 dark:text-white">@_selected.Interest</dd>
                    </div>
                    @if (_selected.Phone is not null)
                    {
                        <div>
                            <dt class="text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500">Phone</dt>
                            <dd class="mt-1 text-sm text-gray-900 dark:text-white">@_selected.Phone</dd>
                        </div>
                    }
                    @if (_selected.Company is not null)
                    {
                        <div>
                            <dt class="text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500">Company</dt>
                            <dd class="mt-1 text-sm text-gray-900 dark:text-white">@_selected.Company</dd>
                        </div>
                    }
                    <div>
                        <dt class="text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500">Submitted</dt>
                        <dd class="mt-1 text-sm text-gray-900 dark:text-white">
                            @_selected.SubmittedAt.ToString("d MMM yyyy 'at' HH:mm")
                        </dd>
                    </div>
                </dl>

                @if (!string.IsNullOrEmpty(_selected.Message))
                {
                    <div class="rounded-xl bg-gray-50 dark:bg-white/5 ring-1 ring-gray-200 dark:ring-white/5 p-5">
                        <p class="text-xs font-semibold uppercase tracking-widest text-gray-400 dark:text-neutral-500 mb-3">Message</p>
                        <p class="text-sm text-gray-800 dark:text-neutral-200 whitespace-pre-wrap leading-relaxed">@_selected.Message</p>
                    </div>
                }
            </div>

            <div class="px-6 py-4 border-t border-gray-100 dark:border-white/5 flex gap-3">
                <a href="mailto:@_selected.Email"
                   class="flex-1 inline-flex items-center justify-center gap-2 rounded-xl px-4 py-2.5
                          text-sm font-semibold bg-accent-hover0 text-white hover:opacity-90 transition-opacity">
                    <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 0 1-2.25 2.25h-15a2.25 2.25 0 0 1-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25m19.5 0v.243a2.25 2.25 0 0 1-1.07 1.916l-7.5 4.615a2.25 2.25 0 0 1-2.36 0L3.32 8.91a2.25 2.25 0 0 1-1.07-1.916V6.75" />
                    </svg>
                    Reply by email
                </a>
                @if (!_selected.IsRead)
                {
                    <button type="button" @onclick="MarkAsRead"
                            class="rounded-xl px-4 py-2.5 text-sm font-medium
                                   ring-1 ring-gray-200 dark:ring-white/10
                                   text-gray-700 dark:text-neutral-300
                                   hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
                        Mark read
                    </button>
                }
            </div>
        </div>
    </div>
}
```

- [ ] **Step 2: Create `Index.razor.cs`**

```csharp
using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Components.Pages.Admin.QrCampaign;

public partial class Index : ComponentBase
{
    [Inject] private FellsideDigitalDbContext Db { get; set; } = default!;

    private QrCampaignStats? _stats;
    private List<QrLead>?    _leads;
    private QrLead?          _selected;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        var scans = await Db.QrScans.ToListAsync();
        var leads = await Db.QrLeads.OrderByDescending(l => l.SubmittedAt).ToListAsync();

        _stats = new QrCampaignStats
        {
            TotalScans = scans.Count,
            ShirtScans = scans.Count(s => s.Source == "shirt"),
            CardScans  = scans.Count(s => s.Source == "card"),
            TotalLeads = leads.Count,
        };

        _leads = leads;
    }

    private void OpenLead(QrLead lead) => _selected = lead;
    private void CloseDrawer()         => _selected = null;

    private async Task MarkAsRead()
    {
        if (_selected is null) return;

        var entity = await Db.QrLeads.FindAsync(_selected.Id);
        if (entity is not null)
        {
            entity.IsRead = true;
            await Db.SaveChangesAsync();
        }

        _selected.IsRead = true;
        StateHasChanged();
    }

    private sealed record QrCampaignStats
    {
        public int TotalScans { get; init; }
        public int ShirtScans { get; init; }
        public int CardScans  { get; init; }
        public int TotalLeads { get; init; }
    }
}
```

- [ ] **Step 3: Verify**

Navigate to `/Admin/QrCampaign` as a `SiteAdmin` user:
- Stats cards show 0s initially (skeleton loader then numbers)
- Scan via `/q/shirt`, submit a lead via `/scan`, reload admin page
- Confirm stats increment correctly (TotalScans = 1, ShirtScans = 1, Leads = 1)
- Lead row shows purple "T-shirt" badge, unread dot, name, interest
- Click "View" → drawer opens with all lead details
- Click "Mark read" → unread dot and row highlight disappear
- Click "Reply by email" → opens mailto link in default mail client

- [ ] **Step 4: Commit**

```
git add src/FellsideDigital.Web/Components/Pages/Admin/QrCampaign/
git commit -m "feat: add QR campaign admin dashboard with scan stats and lead management"
```

---

## Marketing Notes (outside the codebase)

**Discount code:** `LAUNCH26` — honour system, Oliver applies manually at quote time. Change it anytime in `Scan.razor` (one occurrence of the string).

**Offer validity:** 60 days — stated on the success screen. No enforcement needed; just honour it.

**What to print on business cards/t-shirt:**
- No need to print the URL — the QR code IS the URL
- Optional supporting text: _"Scan for an exclusive offer"_ or just a Fellside Digital logo

**Generating QR codes:**
- Go to [qr.io](https://qr.io) or [QR Tiger](https://www.qrtigers.com)
- T-shirt: point at `https://fellsidedigital.co.uk/q/shirt`
- Business card: point at `https://fellsidedigital.co.uk/q/card`
- Download as SVG for print (vector = crisp at any size)
- Minimum print size: 2cm × 2cm for reliable scanning
