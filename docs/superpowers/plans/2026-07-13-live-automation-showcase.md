# Live Automation Showcase Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the standalone Live Automation Showcase into the Fellside Digital site as a Blazor-native feature: an admin big screen at `/live` that reacts in real time as phones submit at `/live/join`, persisting real leads and sending a branded email.

**Architecture:** Real-time uses Blazor Server circuits (the site's existing transport) plus a singleton `LiveShowcaseState` broadcaster — no raw SignalR hub, no client JS transport. The public join page persists a `QrLead` (source `"live"`), publishes to `LiveShowcaseState`, and fires a new branded email via the existing `EmailService`. The big screen subscribes to `LiveShowcaseState` and animates an honest five-stage pipeline in C#.

**Tech Stack:** .NET 10, Blazor Server (Interactive Server), EF Core + PostgreSQL, Microsoft Graph email, QRCoder (new), Tailwind (compiled), xUnit + Testcontainers.

## Global Constraints

- Target framework: `net10.0`. Nullable + ImplicitUsings enabled.
- Reusable UI primitives live in `FellsideDigital.UI`; app pages/services live in `FellsideDigital.Web`.
- Never surface `ex.Message` to users — wrap risky ops in `try/catch` and use `ErrorHandling.LogAndDescribe(Logger, ex, "doing X")`. Inject `ILogger<T>`.
- No hardcoded secrets. Email goes only through `IEmailService`. Data access is EF Core only (no raw SQL).
- Every protected route needs `[Authorize]` with the correct role. Big screen = `SiteAdmin`; join page = anonymous.
- Email templates must use `EmailTheme` helpers — never hardcode colours or button markup. Banned legacy colours: `#fb923c #f97316 #fff7ed #6366f1 #9a3412 #c2410c`.
- Form inputs use `FieldStyles.MarketingInput`; the primary action uses the `accent` colour (`bg-accent`).
- DB-backed tests use `[Collection(PostgresCollection.Name)]` + `PostgresFixture.CreateContext()` (Docker required). Pure-logic tests need no fixture.
- Lead field mapping for a live submission: `Source="live"`, `Interest="Automation"`, `Company=CompanyResolver.Resolve(email)`, everything else null.
- Commit after each task. All work happens on branch `feature/live-automation-showcase`.

---

## File Structure

**New — `src/FellsideDigital.Web`**
- `Services/CompanyResolver.cs` — static: email domain → display company name.
- `Services/EmailValidator.cs` — static: structural email validation.
- `Services/LiveShowcaseState.cs` — singleton broadcaster + `LiveParticipant` / `LiveSnapshot` records.
- `Services/Live/LiveQrCode.cs` — static: build a QR SVG for a URL (QRCoder).
- `Endpoints/LiveShowcaseEndpoints.cs` — `MapLiveShowcase()` → `/api/live/qr.svg`.
- `Components/Pages/Live/Screen.razor` (+ `.razor.cs`) — admin big screen.
- `Components/Pages/Live/Join.razor` (+ `.razor.cs`) — public phone form.

**Modified — `src/FellsideDigital.Web`**
- `FellsideDigital.Web.csproj` — add QRCoder package.
- `Services/Email/EmailTemplates.cs` — add `LiveAutomationWelcome(QrLead)`.
- `Services/IEmailService.cs` + `Services/EmailService.cs` — add `SendLiveAutomationWelcomeAsync(QrLead)`.
- `Extensions/ServiceConfigurationExtensions.cs` — register `LiveShowcaseState` singleton.
- `Extensions/StartupCompositionExtensions.cs` — call `app.MapLiveShowcase()` in `UseFellsideDigitalPlatform`.

**New — `tests/FellsideDigital.Tests`**
- `CompanyResolverTests.cs`, `EmailValidatorTests.cs`, `LiveShowcaseStateTests.cs`, `LiveQrCodeTests.cs`.
- Additions to `EmailTemplateTests.cs` and `QrLeadServiceTests.cs`.

---

### Task 1: Ported input helpers (CompanyResolver + EmailValidator)

**Files:**
- Create: `src/FellsideDigital.Web/Services/CompanyResolver.cs`
- Create: `src/FellsideDigital.Web/Services/EmailValidator.cs`
- Test: `tests/FellsideDigital.Tests/CompanyResolverTests.cs`
- Test: `tests/FellsideDigital.Tests/EmailValidatorTests.cs`

**Interfaces:**
- Produces: `string? CompanyResolver.Resolve(string email)`; `bool EmailValidator.IsValid(string? email)` (both static, namespace `FellsideDigital.Web.Services`).

- [ ] **Step 1: Write the failing tests**

`tests/FellsideDigital.Tests/CompanyResolverTests.cs`:
```csharp
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

public class CompanyResolverTests
{
    [Theory]
    [InlineData("sam@acme.com", "Acme")]
    [InlineData("sam@acme.co.uk", "Acme")]
    [InlineData("a@dept.acme.ac.uk", "Acme")]
    public void Resolve_returns_company_for_business_domains(string email, string expected)
        => Assert.Equal(expected, CompanyResolver.Resolve(email));

    [Theory]
    [InlineData("sam@gmail.com")]
    [InlineData("sam@hotmail.co.uk")]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Resolve_returns_null_for_generic_or_invalid(string email)
        => Assert.Null(CompanyResolver.Resolve(email));
}
```

`tests/FellsideDigital.Tests/EmailValidatorTests.cs`:
```csharp
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

public class EmailValidatorTests
{
    [Theory]
    [InlineData("sam@acme.com")]
    [InlineData("a.b@sub.example.co.uk")]
    public void IsValid_accepts_well_formed(string email) => Assert.True(EmailValidator.IsValid(email));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-at")]
    [InlineData("two@@at.com")]
    [InlineData("trailing@dot.")]
    [InlineData("space in@email.com")]
    public void IsValid_rejects_malformed(string? email) => Assert.False(EmailValidator.IsValid(email));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~CompanyResolverTests|FullyQualifiedName~EmailValidatorTests"`
Expected: FAIL — `CompanyResolver` / `EmailValidator` do not exist.

- [ ] **Step 3: Create the helpers**

`src/FellsideDigital.Web/Services/CompanyResolver.cs`:
```csharp
using System.Globalization;

namespace FellsideDigital.Web.Services;

/// <summary>Derives a display company name from an email domain. Returns null for
/// generic mailbox providers or unparseable addresses.</summary>
public static class CompanyResolver
{
    private static readonly HashSet<string> GenericDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com", "outlook.com", "hotmail.com", "hotmail.co.uk",
        "live.com", "yahoo.com", "yahoo.co.uk", "icloud.com", "me.com", "mac.com",
        "aol.com", "proton.me", "protonmail.com", "gmx.com", "mail.com", "msn.com"
    };

    private static readonly HashSet<string> SecondLevel = new(StringComparer.OrdinalIgnoreCase)
    {
        "co", "com", "org", "net", "ac", "gov", "edu", "ltd", "plc"
    };

    public static string? Resolve(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var at = email.LastIndexOf('@');
        if (at <= 0 || at == email.Length - 1) return null;

        var domain = email[(at + 1)..].Trim().ToLowerInvariant();
        if (domain.Length == 0 || GenericDomains.Contains(domain)) return null;

        var labels = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length < 2) return null;

        var name = labels.Length >= 3 && SecondLevel.Contains(labels[^2])
            ? labels[^3]
            : labels[^2];

        if (name.Length == 0) return null;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
    }
}
```

`src/FellsideDigital.Web/Services/EmailValidator.cs`:
```csharp
namespace FellsideDigital.Web.Services;

/// <summary>Lightweight structural email validation for the public live-join form.</summary>
public static class EmailValidator
{
    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        email = email.Trim();
        if (email.Contains(' ')) return false;

        var at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1) return false;

        var domain = email[(at + 1)..];
        if (!domain.Contains('.') || domain.StartsWith('.') || domain.EndsWith('.')) return false;

        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~CompanyResolverTests|FullyQualifiedName~EmailValidatorTests"`
Expected: PASS (all cases).

- [ ] **Step 5: Commit**

```bash
git add src/FellsideDigital.Web/Services/CompanyResolver.cs src/FellsideDigital.Web/Services/EmailValidator.cs tests/FellsideDigital.Tests/CompanyResolverTests.cs tests/FellsideDigital.Tests/EmailValidatorTests.cs
git commit -m "feat: add CompanyResolver and EmailValidator helpers for live showcase"
```

---

### Task 2: LiveShowcaseState broadcaster

**Files:**
- Create: `src/FellsideDigital.Web/Services/LiveShowcaseState.cs`
- Test: `tests/FellsideDigital.Tests/LiveShowcaseStateTests.cs`

**Interfaces:**
- Produces:
  - `record LiveParticipant(string Name, string? Company, DateTimeOffset JoinedAt)`
  - `record LiveSnapshot(int Count, IReadOnlyList<LiveParticipant> Recent)`
  - `LiveShowcaseState`: `void Publish(LiveParticipant p)`, `LiveSnapshot Snapshot()`, `void Reset()`, `event Action<LiveParticipant>? ParticipantJoined`, `event Action? ResetRequested`.
- Consumed by: Task 5 (Screen), Task 6 (Join), Task 7 (registration).

- [ ] **Step 1: Write the failing test**

`tests/FellsideDigital.Tests/LiveShowcaseStateTests.cs`:
```csharp
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

public class LiveShowcaseStateTests
{
    private static LiveParticipant P(string name) => new(name, null, DateTimeOffset.UtcNow);

    [Fact]
    public void Publish_increments_count_and_raises_event()
    {
        var state = new LiveShowcaseState();
        LiveParticipant? seen = null;
        state.ParticipantJoined += p => seen = p;

        state.Publish(P("Sam"));

        Assert.Equal(1, state.Snapshot().Count);
        Assert.Equal("Sam", seen?.Name);
    }

    [Fact]
    public void Snapshot_returns_recent_newest_first_capped_at_eight()
    {
        var state = new LiveShowcaseState();
        for (var i = 0; i < 10; i++) state.Publish(P($"P{i}"));

        var snap = state.Snapshot();

        Assert.Equal(10, snap.Count);
        Assert.Equal(8, snap.Recent.Count);
        Assert.Equal("P9", snap.Recent[0].Name);
    }

    [Fact]
    public void Reset_clears_state_and_raises_event()
    {
        var state = new LiveShowcaseState();
        state.Publish(P("Sam"));
        var raised = false;
        state.ResetRequested += () => raised = true;

        state.Reset();

        Assert.Equal(0, state.Snapshot().Count);
        Assert.Empty(state.Snapshot().Recent);
        Assert.True(raised);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~LiveShowcaseStateTests"`
Expected: FAIL — `LiveShowcaseState` not defined.

- [ ] **Step 3: Implement**

`src/FellsideDigital.Web/Services/LiveShowcaseState.cs`:
```csharp
namespace FellsideDigital.Web.Services;

public record LiveParticipant(string Name, string? Company, DateTimeOffset JoinedAt);

public record LiveSnapshot(int Count, IReadOnlyList<LiveParticipant> Recent);

/// <summary>
/// In-memory, process-wide broadcaster for the live automation showcase. Phone joins
/// publish participants; the admin big screen subscribes. Count is intentionally
/// ephemeral (resets on restart or via <see cref="Reset"/>); persisted leads live in
/// the database via QrLeadService.
/// </summary>
public sealed class LiveShowcaseState
{
    private const int MaxRecent = 8;
    private readonly object _lock = new();
    private readonly List<LiveParticipant> _recent = new();
    private int _count;

    public event Action<LiveParticipant>? ParticipantJoined;
    public event Action? ResetRequested;

    public void Publish(LiveParticipant p)
    {
        lock (_lock)
        {
            _count++;
            _recent.Insert(0, p);
            if (_recent.Count > MaxRecent) _recent.RemoveAt(_recent.Count - 1);
        }
        ParticipantJoined?.Invoke(p);
    }

    public LiveSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new LiveSnapshot(_count, _recent.ToList());
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _count = 0;
            _recent.Clear();
        }
        ResetRequested?.Invoke();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~LiveShowcaseStateTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/FellsideDigital.Web/Services/LiveShowcaseState.cs tests/FellsideDigital.Tests/LiveShowcaseStateTests.cs
git commit -m "feat: add LiveShowcaseState real-time broadcaster"
```

---

### Task 3: QR SVG helper + QRCoder package

**Files:**
- Modify: `src/FellsideDigital.Web/FellsideDigital.Web.csproj` (add package)
- Create: `src/FellsideDigital.Web/Services/Live/LiveQrCode.cs`
- Test: `tests/FellsideDigital.Tests/LiveQrCodeTests.cs`

**Interfaces:**
- Produces: `string LiveQrCode.Svg(string url)` (static, namespace `FellsideDigital.Web.Services.Live`) → an `<svg>` document string.
- Consumed by: Task 8 (`/api/live/qr.svg` endpoint).

- [ ] **Step 1: Add the QRCoder package**

Run: `dotnet add src/FellsideDigital.Web package QRCoder --version 1.6.0`
Expected: adds `<PackageReference Include="QRCoder" Version="1.6.0" />` to the csproj.

- [ ] **Step 2: Write the failing test**

`tests/FellsideDigital.Tests/LiveQrCodeTests.cs`:
```csharp
using FellsideDigital.Web.Services.Live;

namespace FellsideDigital.Tests;

public class LiveQrCodeTests
{
    [Fact]
    public void Svg_returns_an_svg_document()
    {
        var svg = LiveQrCode.Svg("https://fellsidedigital.co.uk/live/join");

        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~LiveQrCodeTests"`
Expected: FAIL — `LiveQrCode` not defined.

- [ ] **Step 4: Implement**

`src/FellsideDigital.Web/Services/Live/LiveQrCode.cs`:
```csharp
using QRCoder;

namespace FellsideDigital.Web.Services.Live;

/// <summary>Renders a scannable QR code as an inline SVG for the big screen.</summary>
public static class LiveQrCode
{
    public static string Svg(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        return new SvgQRCode(data).GetGraphic(10, "#0f172a", "#ffffff", drawQuietZones: true);
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~LiveQrCodeTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/FellsideDigital.Web/FellsideDigital.Web.csproj src/FellsideDigital.Web/Services/Live/LiveQrCode.cs tests/FellsideDigital.Tests/LiveQrCodeTests.cs
git commit -m "feat: add QRCoder-backed LiveQrCode SVG helper"
```

---

### Task 4: Live automation welcome email

**Files:**
- Modify: `src/FellsideDigital.Web/Services/Email/EmailTemplates.cs` (add method near `QrLeadDiscount`)
- Modify: `src/FellsideDigital.Web/Services/IEmailService.cs`
- Modify: `src/FellsideDigital.Web/Services/EmailService.cs` (add method near `SendQrLeadDiscountAsync`)
- Test: `tests/FellsideDigital.Tests/EmailTemplateTests.cs` (add a fact)

**Interfaces:**
- Produces: `string EmailTemplates.LiveAutomationWelcome(QrLead lead)`; `Task IEmailService.SendLiveAutomationWelcomeAsync(QrLead lead)`.
- Consumed by: Task 6 (Join page).

- [ ] **Step 1: Write the failing test**

Add to `tests/FellsideDigital.Tests/EmailTemplateTests.cs`:
```csharp
    [Fact]
    public void LiveAutomationWelcome_is_branded_and_personalised()
    {
        var html = EmailTemplates.LiveAutomationWelcome(new QrLead
        {
            Source = "live", Name = "Sam", Email = "sam@acme.com", Interest = "Automation",
        });

        AssertNoBannedColours(html);
        Assert.Contains("Sam", html);
        Assert.Contains("15%", html);
        Assert.Contains("cid:fellside-logo", html); // inline logo from EmailTheme.Layout
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~EmailTemplateTests.LiveAutomationWelcome_is_branded_and_personalised"`
Expected: FAIL — `EmailTemplates.LiveAutomationWelcome` not defined.

- [ ] **Step 3: Add the template**

In `src/FellsideDigital.Web/Services/Email/EmailTemplates.cs`, under the `// ── Marketing ──` region (near `QrLeadDiscount`):
```csharp
    public static string LiveAutomationWelcome(QrLead lead) => EmailTheme.Layout($"""
        {H2($"Hi {lead.Name}, that was live. ⚡")}
        {P("You just triggered a real automation from our talk — your details were captured, "
           + "enriched and this email sent, in seconds. This is exactly the kind of thing we build for our clients.")}
        {P("Interested in a project? Mention the live demo and get <strong>15% off</strong> your first piece of work with us.")}
        <div style="margin:0 0 8px;">{EmailTheme.Button("https://fellsidedigital.co.uk/scan?from=live", "Claim your 15% off →")}</div>
        """);
```

- [ ] **Step 4: Add the interface method**

In `src/FellsideDigital.Web/Services/IEmailService.cs`, after `SendQrLeadDiscountAsync`:
```csharp
    Task SendLiveAutomationWelcomeAsync(QrLead lead);
```

- [ ] **Step 5: Add the service method**

In `src/FellsideDigital.Web/Services/EmailService.cs`, after `SendQrLeadDiscountAsync`:
```csharp
    public Task SendLiveAutomationWelcomeAsync(QrLead lead) =>
        SendAsync(
            lead.Email,
            "You just triggered a live automation ⚡",
            EmailTemplates.LiveAutomationWelcome(lead));
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~EmailTemplateTests"`
Expected: PASS (new fact + existing email facts still green).

- [ ] **Step 7: Commit**

```bash
git add src/FellsideDigital.Web/Services/Email/EmailTemplates.cs src/FellsideDigital.Web/Services/IEmailService.cs src/FellsideDigital.Web/Services/EmailService.cs tests/FellsideDigital.Tests/EmailTemplateTests.cs
git commit -m "feat: add live automation welcome email"
```

---

### Task 5: Live lead persistence guard test

**Files:**
- Test: `tests/FellsideDigital.Tests/QrLeadServiceTests.cs` (add a fact)

**Interfaces:**
- Consumes: existing `QrLeadService.CreateLeadAsync` / `GetLeadsAsync`.
- Confirms the design's claim that a `"live"` lead persists and surfaces in the admin dashboard with **no** production change.

- [ ] **Step 1: Write the test**

Add to `tests/FellsideDigital.Tests/QrLeadServiceTests.cs`:
```csharp
    [Fact]
    public async Task Live_source_lead_persists_and_appears_in_leads_list()
    {
        await using var db = fx.CreateContext();
        var sut = new QrLeadService(db);

        var lead = await sut.CreateLeadAsync(new QrLead
        {
            Source = "live", Name = "Live Sam", Email = "sam@acme.com",
            Company = "Acme", Interest = "Automation",
        });

        var all = await sut.GetLeadsAsync();
        Assert.Contains(all, l => l.Id == lead.Id && l.Source == "live");
    }
```

- [ ] **Step 2: Run test to verify it passes (Docker required)**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~QrLeadServiceTests.Live_source_lead_persists_and_appears_in_leads_list"`
Expected: PASS. (If it fails to start, ensure Docker is running — Testcontainers needs it.)

- [ ] **Step 3: Commit**

```bash
git add tests/FellsideDigital.Tests/QrLeadServiceTests.cs
git commit -m "test: guard that live-source leads persist and list"
```

---

### Task 6: Register broadcaster + build the phone join page

**Files:**
- Modify: `src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs` (register singleton)
- Create: `src/FellsideDigital.Web/Components/Pages/Live/Join.razor`
- Create: `src/FellsideDigital.Web/Components/Pages/Live/Join.razor.cs`

**Interfaces:**
- Consumes: `LiveShowcaseState`, `IQrLeadService`, `IEmailService`, `CompanyResolver`, `EmailValidator`, `LiveParticipant`.
- Produces: public route `/live/join`.

- [ ] **Step 1: Register the broadcaster**

In `src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs`, inside `ConfigurePortalServices`, next to `services.AddScoped<IQrLeadService, QrLeadService>();`, add:
```csharp
        services.AddSingleton<LiveShowcaseState>();
```

- [ ] **Step 2: Create the join page markup**

`src/FellsideDigital.Web/Components/Pages/Live/Join.razor`:
```razor
@page "/live/join"
@rendermode InteractiveServer
@using FellsideDigital.UI.Components.Forms

<PageTitle>Trigger a live automation — Fellside Digital</PageTitle>

<div class="min-h-screen bg-white dark:bg-neutral-950 text-slate-900 dark:text-white flex items-center justify-center px-6">
    <div class="w-full max-w-sm">
        @if (!_submitted)
        {
            <div class="text-center mb-8">
                <div class="text-xl font-bold text-accent">Fellside Digital</div>
                <h1 class="text-2xl font-bold mt-3">Trigger a live automation</h1>
                <p class="text-slate-500 dark:text-neutral-400 mt-2">Enter your details, hit go, then look up at the screen.</p>
            </div>

            <div class="space-y-4">
                <input @bind="_name" type="text" placeholder="Your name" class="@FieldStyles.MarketingInput" />
                <input @bind="_email" type="email" placeholder="you@company.com" class="@FieldStyles.MarketingInput" />

                @if (!string.IsNullOrEmpty(_error))
                {
                    <p class="text-sm text-red-500 dark:text-red-400">@_error</p>
                }

                <button type="button" @onclick="SubmitAsync" disabled="@_saving"
                        class="w-full rounded-xl bg-accent px-6 py-3 text-sm font-semibold text-white hover:opacity-90 transition-opacity disabled:opacity-50">
                    @(_saving ? "Triggering…" : "Trigger it →")
                </button>
            </div>
        }
        else
        {
            <div class="text-center">
                <div class="text-6xl mb-4">⚡</div>
                <h2 class="text-2xl font-bold">You're live!</h2>
                <p class="text-slate-500 dark:text-neutral-400 mt-2">@_successMessage</p>
            </div>
        }
    </div>
</div>
```

- [ ] **Step 3: Create the join page code-behind**

`src/FellsideDigital.Web/Components/Pages/Live/Join.razor.cs`:
```csharp
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Live;

public partial class Join : ComponentBase
{
    [Inject] private IQrLeadService QrLeadService { get; set; } = default!;
    [Inject] private IEmailService EmailService { get; set; } = default!;
    [Inject] private LiveShowcaseState Live { get; set; } = default!;
    [Inject] private ILogger<Join> Logger { get; set; } = default!;

    private string _name = "";
    private string _email = "";
    private string _error = "";
    private bool _saving;
    private bool _submitted;
    private string _successMessage = "Look up at the big screen — that's you.";

    private async Task SubmitAsync()
    {
        _error = "";
        var name = _name.Trim();
        var email = _email.Trim();

        if (name.Length == 0 || !EmailValidator.IsValid(email))
        {
            _error = "Please enter your name and a valid email address.";
            return;
        }

        _saving = true;
        var company = CompanyResolver.Resolve(email);

        var lead = new QrLead
        {
            Source = "live",
            Name = name,
            Email = email,
            Company = company,
            Interest = "Automation",
        };

        try
        {
            await QrLeadService.CreateLeadAsync(lead);
        }
        catch (Exception ex)
        {
            _error = ErrorHandling.LogAndDescribe(Logger, ex, "triggering the automation");
            _saving = false;
            return;
        }

        Live.Publish(new LiveParticipant(name, company, DateTimeOffset.UtcNow));

        var emailed = true;
        try
        {
            await EmailService.SendLiveAutomationWelcomeAsync(lead);
        }
        catch (Exception ex)
        {
            emailed = false;
            Logger.LogError(ex, "Live welcome email failed for {Email}", email);
        }

        _successMessage = emailed
            ? "Look up at the big screen — and check your inbox in a moment."
            : "Look up at the big screen — that's you.";
        _submitted = true;
        _saving = false;
    }
}
```

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build src/FellsideDigital.Web`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs "src/FellsideDigital.Web/Components/Pages/Live/Join.razor" "src/FellsideDigital.Web/Components/Pages/Live/Join.razor.cs"
git commit -m "feat: add public live-join page and register broadcaster"
```

---

### Task 7: Build the admin big screen

**Files:**
- Create: `src/FellsideDigital.Web/Components/Pages/Live/Screen.razor`
- Create: `src/FellsideDigital.Web/Components/Pages/Live/Screen.razor.cs`

**Interfaces:**
- Consumes: `LiveShowcaseState`, `LiveParticipant`. Renders `<img src="/api/live/qr.svg">` (endpoint arrives in Task 8).
- Produces: admin route `/live` (`[Authorize(Roles="SiteAdmin")]`).

- [ ] **Step 1: Create the screen markup**

`src/FellsideDigital.Web/Components/Pages/Live/Screen.razor`:
```razor
@page "/live"
@rendermode InteractiveServer
@attribute [Authorize(Roles = "SiteAdmin")]
@implements IDisposable
@using Microsoft.AspNetCore.Authorization

<PageTitle>Live Automation — Fellside Digital</PageTitle>

<div class="min-h-screen bg-white dark:bg-neutral-950 text-slate-900 dark:text-white px-8 py-10">
    <header class="flex items-center justify-between mb-10">
        <div class="text-2xl font-bold text-accent">Fellside Digital</div>
        <div class="flex items-center gap-4">
            <span class="text-sm font-medium text-slate-400 uppercase tracking-widest">Live Automation Demo</span>
            <button @onclick="ResetAsync"
                    class="rounded-lg border border-slate-200 dark:border-white/10 px-3 py-1.5 text-xs text-slate-500 dark:text-neutral-400 hover:border-slate-300 dark:hover:border-white/20 transition">
                Reset
            </button>
        </div>
    </header>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-10">
        <div class="lg:col-span-1 flex flex-col items-center text-center">
            <h1 class="text-3xl font-bold mb-2">Scan to see it live</h1>
            <p class="text-slate-500 dark:text-neutral-400 mb-6">Point your camera here and watch this screen react in real time.</p>
            <div class="p-5 rounded-3xl border-4 border-accent shadow-xl bg-white">
                <img src="/api/live/qr.svg" alt="Scan to join" class="w-64 h-64" />
            </div>
            <div class="mt-6 text-6xl font-black text-accent tabular-nums">@_count</div>
            <div class="text-slate-400 uppercase tracking-widest text-xs">people joined</div>
        </div>

        <div class="lg:col-span-2">
            <div class="rounded-3xl border border-slate-200 dark:border-white/10 shadow-lg p-8 min-h-[360px] flex flex-col justify-center">
                @if (_current is null)
                {
                    <div class="text-center text-slate-400">
                        <div class="text-2xl font-semibold">Waiting for the next lead…</div>
                        <div class="mt-2">Scan the QR code to trigger a live automation.</div>
                    </div>
                }
                else
                {
                    <div class="text-center mb-8">
                        <div class="text-sm uppercase tracking-widest text-slate-400">Now processing</div>
                        <div class="text-4xl font-bold">@_current.Name</div>
                    </div>
                    <div class="space-y-3">
                        @for (var i = 0; i < _stages.Length; i++)
                        {
                            var done = i < _activeStage || (i == _activeStage && _stageComplete);
                            var active = i == _activeStage;
                            <div class="@StageRowClass(active, done)">
                                <span class="@StageDotClass(active, done)"></span>
                                <span class="font-medium">@_stages[i]</span>
                                <span class="ml-auto font-bold text-accent @(done ? "opacity-100" : "opacity-0") transition-opacity">✓</span>
                            </div>
                        }
                    </div>
                }
            </div>

            <div class="mt-8">
                <div class="text-xs uppercase tracking-widest text-slate-400 mb-3">Live feed</div>
                <ul class="space-y-2">
                    @foreach (var p in _feed)
                    {
                        <li class="flex items-center gap-3 text-slate-600 dark:text-neutral-300">
                            <span class="w-2 h-2 rounded-full bg-accent"></span>
                            <span class="font-semibold text-slate-900 dark:text-white">@p.Name</span>
                            @if (!string.IsNullOrEmpty(p.Company))
                            {
                                <span class="text-slate-400">· @p.Company</span>
                            }
                            <span class="text-slate-300 dark:text-neutral-600 text-sm ml-auto">just now</span>
                        </li>
                    }
                </ul>
            </div>
        </div>
    </div>
</div>
```

- [ ] **Step 2: Create the screen code-behind**

`src/FellsideDigital.Web/Components/Pages/Live/Screen.razor.cs`:
```csharp
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Live;

public partial class Screen : ComponentBase, IDisposable
{
    [Inject] private LiveShowcaseState Live { get; set; } = default!;

    private const int MaxFeed = 8;
    private readonly Queue<LiveParticipant> _queue = new();
    private readonly List<LiveParticipant> _feed = new();
    private int _count;
    private bool _running;

    private LiveParticipant? _current;
    private int _activeStage = -1;
    private bool _stageComplete;
    private string[] _stages = [];

    protected override void OnInitialized()
    {
        var snap = Live.Snapshot();
        _count = snap.Count;
        _feed.AddRange(snap.Recent);
        Live.ParticipantJoined += OnJoined;
        Live.ResetRequested += OnReset;
    }

    private void OnJoined(LiveParticipant p) => _ = InvokeAsync(async () =>
    {
        _count++;
        _feed.Insert(0, p);
        if (_feed.Count > MaxFeed) _feed.RemoveAt(_feed.Count - 1);
        _queue.Enqueue(p);
        StateHasChanged();
        await DrainAsync();
    });

    private void OnReset() => _ = InvokeAsync(() =>
    {
        _count = 0;
        _feed.Clear();
        _queue.Clear();
        _current = null;
        _activeStage = -1;
        StateHasChanged();
    });

    private async Task ResetAsync()
    {
        Live.Reset(); // raises ResetRequested → OnReset marshals the UI update
        await Task.CompletedTask;
    }

    private async Task DrainAsync()
    {
        if (_running) return;
        _running = true;

        while (_queue.Count > 0)
        {
            var p = _queue.Dequeue();
            _current = p;
            _stages =
            [
                "Lead captured",
                $"Enriching {(string.IsNullOrEmpty(p.Company) ? "profile" : p.Company)}…",
                "CRM record created",
                "✉ Welcome email sent",
                "✓ Done",
            ];

            for (var i = 0; i < _stages.Length; i++)
            {
                _activeStage = i;
                _stageComplete = false;
                StateHasChanged();
                await Task.Delay(900);
                _stageComplete = true;
                StateHasChanged();
            }

            await Task.Delay(1200);
            _current = null;
            _activeStage = -1;
            StateHasChanged();
        }

        _running = false;
    }

    private static string StageRowClass(bool active, bool done) =>
        "flex items-center gap-4 rounded-xl px-4 py-3 border transition-all duration-300 " +
        (active || done
            ? "text-slate-900 dark:text-white border-accent bg-accent/5"
            : "text-slate-400 border-slate-100 dark:border-white/5");

    private static string StageDotClass(bool active, bool done) =>
        "w-3 h-3 rounded-full transition-colors " + (active || done ? "bg-accent" : "bg-slate-300");

    public void Dispose()
    {
        Live.ParticipantJoined -= OnJoined;
        Live.ResetRequested -= OnReset;
    }
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build src/FellsideDigital.Web`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add "src/FellsideDigital.Web/Components/Pages/Live/Screen.razor" "src/FellsideDigital.Web/Components/Pages/Live/Screen.razor.cs"
git commit -m "feat: add admin live big-screen with animated pipeline"
```

---

### Task 8: QR endpoint wiring

**Files:**
- Create: `src/FellsideDigital.Web/Endpoints/LiveShowcaseEndpoints.cs`
- Modify: `src/FellsideDigital.Web/Extensions/StartupCompositionExtensions.cs` (call `MapLiveShowcase`)

**Interfaces:**
- Consumes: `LiveQrCode.Svg`. Produces: `GET /api/live/qr.svg`.

- [ ] **Step 1: Create the endpoint**

`src/FellsideDigital.Web/Endpoints/LiveShowcaseEndpoints.cs`:
```csharp
using FellsideDigital.Web.Services.Live;

namespace FellsideDigital.Web.Endpoints;

public static class LiveShowcaseEndpoints
{
    /// <summary>Renders the QR that points phones at the public /live/join page.</summary>
    public static void MapLiveShowcase(this WebApplication app)
    {
        app.MapGet("/api/live/qr.svg", (HttpContext ctx, IConfiguration cfg) =>
        {
            var baseUrl = (cfg["PUBLIC_BASE_URL"] ?? $"{ctx.Request.Scheme}://{ctx.Request.Host}").TrimEnd('/');
            var svg = LiveQrCode.Svg($"{baseUrl}/live/join");
            return Results.Content(svg, "image/svg+xml");
        });
    }
}
```

- [ ] **Step 2: Wire it into the pipeline**

In `src/FellsideDigital.Web/Extensions/StartupCompositionExtensions.cs`, add the using at the top:
```csharp
using FellsideDigital.Web.Endpoints;
```
Then in `UseFellsideDigitalPlatform`, directly after `app.MapQrRedirects();`, add:
```csharp
        app.MapLiveShowcase();
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build src/FellsideDigital.Web`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/FellsideDigital.Web/Endpoints/LiveShowcaseEndpoints.cs src/FellsideDigital.Web/Extensions/StartupCompositionExtensions.cs
git commit -m "feat: map /api/live/qr.svg endpoint"
```

---

### Task 9: Full-suite + end-to-end verification

**Files:** none (verification only).

- [ ] **Step 1: Run the whole test suite (Docker running)**

Run: `dotnet test`
Expected: All tests pass, including new `CompanyResolverTests`, `EmailValidatorTests`, `LiveShowcaseStateTests`, `LiveQrCodeTests`, the `EmailTemplateTests` addition, and the `QrLeadServiceTests` addition.

- [ ] **Step 2: Manual end-to-end walk-through**

Run: `dotnet run --project src/FellsideDigital.Web --launch-profile http` (app at `http://localhost:5185`).
Verify:
1. `GET /api/live/qr.svg` returns an SVG (open in browser).
2. Visiting `/live` while **not** signed in as `SiteAdmin` redirects to login (auth enforced).
3. Sign in as admin, open `/live` — QR, count `0`, idle pipeline, empty feed.
4. In a second browser/phone, open `/live/join`, submit name + a business email → success message.
5. The `/live` screen (first browser) animates the five-stage pipeline, count increments to `1`, and the feed shows the name · resolved company.
6. Click **Reset** on `/live` → count returns to `0`, feed clears.
7. `/Admin/QrCampaign` lists the new lead with source `live`.
8. If email is configured, the recipient gets the "You just triggered a live automation ⚡" email; if unconfigured in Development, the send is logged and skipped (no error to the user).

- [ ] **Step 3: Finalise**

Use the superpowers:finishing-a-development-branch skill to decide merge/PR/cleanup.

---

## Self-Review

- **Spec coverage:** Routes `/live`, `/live/join`, `/api/live/qr.svg` → Tasks 7, 6, 8. `LiveShowcaseState` → Task 2. `CompanyResolver`/`EmailValidator` → Task 1. Persistence as `QrLead` source `live` → Tasks 6 + 5. New branded email → Task 4. QRCoder → Task 3. Admin-only screen + public join auth → Tasks 7/6. Reset control + snapshot replay + animation improvements → Tasks 2/7. Testing matrix → Tasks 1–5, 9. All spec sections covered.
- **Placeholder scan:** No TBD/TODO; every code step shows complete code; commands have expected output.
- **Type consistency:** `LiveParticipant(string, string?, DateTimeOffset)`, `LiveSnapshot(int, IReadOnlyList<LiveParticipant>)`, `Publish/Snapshot/Reset`, `ParticipantJoined`/`ResetRequested`, `LiveQrCode.Svg(string)`, `EmailTemplates.LiveAutomationWelcome(QrLead)`, `SendLiveAutomationWelcomeAsync(QrLead)`, `CompanyResolver.Resolve(string)`, `EmailValidator.IsValid(string?)` used consistently across tasks.
