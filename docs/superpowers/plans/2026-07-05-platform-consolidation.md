# Platform Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unify the email system behind one interface, guarantee password-change access for admins and clients, fix the flaky home-page project iframes, enforce the `.UI` component library across all pages, fix admin/portal design defects, and verify SEO.

**Architecture:** Blazor Server (.NET, Interactive Server render mode) with a three-project split: `FellsideDigital.Domain` (enums), `FellsideDigital.UI` (shared Razor components), `FellsideDigital.Web` (app). Email goes out via Microsoft Graph (`EmailService`). All work is refactoring + defect fixes — **zero intentional visual redesign**.

**Tech Stack:** ASP.NET Core Identity, EF Core + PostgreSQL, Tailwind CSS, Microsoft Graph SDK, xUnit (+ Testcontainers for DB tests).

**Spec:** `docs/superpowers/specs/2026-07-05-platform-consolidation-design.md`

## Global Constraints

- Branch: `feature/platform-consolidation` (already created; spec committed).
- Build command (WSL → Windows dotnet): `dotnet.exe build FellsideDigitalWebsite.sln`. Ignore any flaky `App.razor 'Html' CS0103` generator error if it appears once and disappears on rebuild (known artifact).
- Test command: `dotnet.exe test tests/FellsideDigital.Tests --no-build` after a build. DB-backed tests need Docker; pure-logic tests always run.
- Never surface exception detail to users; use `ErrorHandling.LogAndDescribe`.
- No secrets in code, EF Core only (no raw SQL), keep cookie policy `SameSite=Lax`.
- Visual output must be preserved — when swapping to shared components, the resulting Tailwind classes must be identical or visually equivalent to what the page had.
- New `.UI` namespace folders need a `@using` line in `src/FellsideDigital.Web/Components/_Imports.razor`.
- Commit after every task (scoped `git add`, message ends with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`).

---

### Task 1: `IEmailService` interface, all consumers switch to it

**Files:**
- Create: `src/FellsideDigital.Web/Services/IEmailService.cs`
- Modify: `src/FellsideDigital.Web/Services/EmailService.cs` (class declaration line 13)
- Modify: `src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs:73`
- Modify: `src/FellsideDigital.Web/Extensions/AuthenticationExtensions.cs:85`
- Modify (consumer type swap only): `src/FellsideDigital.Web/Components/Account/Pages/Register.razor:16`, `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor.cs:21`, `src/FellsideDigital.Web/Components/Pages/Marketing/Contact.razor.cs:15`, `src/FellsideDigital.Web/Components/Pages/Marketing/Scan.razor.cs:10`, `src/FellsideDigital.Web/Services/InvitationService.cs:11`, `src/FellsideDigital.Web/Services/InvoiceService.cs:16`, `src/FellsideDigital.Web/Services/ProjectDocumentService.cs:16`

**Interfaces:**
- Consumes: existing `EmailService` public methods (unchanged signatures).
- Produces: `IEmailService` — the app-wide email abstraction. Later tasks (2) depend on it existing. Signature list below is the complete contract.

- [ ] **Step 1: Create the interface**

`src/FellsideDigital.Web/Services/IEmailService.cs`:

```csharp
using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Services;

/// <summary>
/// The single email abstraction for the app. Covers Identity's built-in flows
/// (via IEmailSender&lt;ApplicationUser&gt;) and every transactional email we send,
/// so all mail runs through one pipeline (EmailService → Microsoft Graph).
/// </summary>
public interface IEmailService : IEmailSender<ApplicationUser>
{
    Task SendInvitationAsync(ClientInvitation invitation, string registrationUrl);
    Task SendClientRegisteredNotificationAsync(ApplicationUser user);
    Task SendWelcomeEmailAsync(ApplicationUser user);
    Task SendContactEnquiryAsync(ContactEnquiry enquiry);
    Task SendQrLeadNotificationAsync(QrLead lead);
    Task SendQrLeadDiscountAsync(QrLead lead);
    Task SendDocumentAddedAsync(ApplicationUser client, ClientProject project, string documentTitle, string portalUrl);
    Task SendInvoiceAddedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl);
    Task SendInvoiceUpdatedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl);
    Task SendInvoiceStatusChangedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl);
    Task SendTestimonialRequestAsync(ApplicationUser client, ClientProject project, string testimonialUrl);
}
```

- [ ] **Step 2: Implement it on EmailService**

In `EmailService.cs` change line 13:

```csharp
public class EmailService : IEmailService
```

(`IEmailService` already extends `IEmailSender<ApplicationUser>`, so nothing else changes.)

- [ ] **Step 3: Register the interface**

In `ServiceConfigurationExtensions.cs` `ConfigureEmailService`, after `services.AddSingleton<EmailService>();` (line 73) add:

```csharp
services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<EmailService>());
```

In `AuthenticationExtensions.cs` line 85 change to:

```csharp
services.AddSingleton<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<IEmailService>());
```

- [ ] **Step 4: Switch every consumer to the interface**

Exact edits (type name only — call sites are unchanged):
- `Register.razor:16`: `@inject EmailService EmailService` → `@inject IEmailService EmailService`
- `Detail.razor.cs:21`: `[Inject] private EmailService Email` → `[Inject] private IEmailService Email`
- `Contact.razor.cs:15`: `[Inject] private EmailService EmailService` → `[Inject] private IEmailService EmailService`
- `Scan.razor.cs:10`: `[Inject] private EmailService   EmailService` → `[Inject] private IEmailService EmailService`
- `InvitationService.cs:11`: primary-ctor param `EmailService emailService` → `IEmailService emailService`
- `InvoiceService.cs:16`: `EmailService email` → `IEmailService email`
- `ProjectDocumentService.cs:16`: `EmailService email` → `IEmailService email`

Then confirm nothing else injects the concrete class:

Run: `grep -rn "EmailService" src/FellsideDigital.Web --include="*.cs" --include="*.razor" | grep -v obj/ | grep -vE "IEmailService|Services/EmailService.cs|Services/Email/|AddSingleton<EmailService>"`
Expected: only the two extension-method registration lines and comments; **no** remaining `@inject EmailService` / `[Inject] private EmailService` / ctor `EmailService` params. If any appear (e.g. `TestimonialService`), apply the same type swap.

- [ ] **Step 5: Build**

Run: `dotnet.exe build FellsideDigitalWebsite.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/FellsideDigital.Web/Services/IEmailService.cs src/FellsideDigital.Web/Services/EmailService.cs src/FellsideDigital.Web/Extensions src/FellsideDigital.Web/Components/Account/Pages/Register.razor src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor.cs src/FellsideDigital.Web/Components/Pages/Marketing/Contact.razor.cs src/FellsideDigital.Web/Components/Pages/Marketing/Scan.razor.cs src/FellsideDigital.Web/Services/InvitationService.cs src/FellsideDigital.Web/Services/InvoiceService.cs src/FellsideDigital.Web/Services/ProjectDocumentService.cs
git commit -m "refactor: put all email behind IEmailService interface"
```

---

### Task 2: `EmailSettings.IsConfigured`, dev fallback, delete the no-op sender

**Files:**
- Modify: `src/FellsideDigital.Web/Models/EmailSettings.cs`
- Modify: `src/FellsideDigital.Web/Services/EmailService.cs:116-126` (SendAsync guard)
- Modify: `src/FellsideDigital.Web/Components/Account/Pages/RegisterConfirmation.razor`
- Delete: `src/FellsideDigital.Web/Components/Account/IdentityNoOpEmailSender.cs`
- Modify: `CLAUDE.md` (stale email notes)
- Test: `tests/FellsideDigital.Tests/EmailSettingsTests.cs` (new)

**Interfaces:**
- Consumes: `IEmailService` from Task 1 (RegisterConfirmation keeps injecting `IEmailSender<ApplicationUser>`; only the type-check changes).
- Produces: `EmailSettings.IsConfigured` (bool, computed property) — used by `EmailService` and `RegisterConfirmation`.

- [ ] **Step 1: Write failing tests**

`tests/FellsideDigital.Tests/EmailSettingsTests.cs`:

```csharp
using FellsideDigital.Web.Models;

namespace FellsideDigital.Tests;

public class EmailSettingsTests
{
    private static EmailSettings Complete() => new()
    {
        TenantId = "t", ClientId = "c", ClientSecret = "s",
        FromAddress = "hello@example.com", AdminEmail = "admin@example.com"
    };

    [Fact]
    public void IsConfigured_true_when_all_graph_fields_present()
        => Assert.True(Complete().IsConfigured);

    [Theory]
    [InlineData(nameof(EmailSettings.TenantId))]
    [InlineData(nameof(EmailSettings.ClientId))]
    [InlineData(nameof(EmailSettings.ClientSecret))]
    [InlineData(nameof(EmailSettings.FromAddress))]
    public void IsConfigured_false_when_any_graph_field_missing(string missing)
    {
        var s = Complete();
        typeof(EmailSettings).GetProperty(missing)!.SetValue(s, "  ");
        Assert.False(s.IsConfigured);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet.exe build FellsideDigitalWebsite.sln`
Expected: FAILS — `'EmailSettings' does not contain a definition for 'IsConfigured'`.

- [ ] **Step 3: Add the property**

In `EmailSettings.cs`, after `AdminEmail`:

```csharp
    /// <summary>True when every field needed to send via Microsoft Graph is present.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(FromAddress);
```

- [ ] **Step 4: Use it in EmailService and add the dev no-op**

`EmailService` ctor gains `IHostEnvironment` awareness via the existing `IWebHostEnvironment _env` (already injected). Replace the guard at the top of `SendAsync` (lines 118-126):

```csharp
        if (!_settings.IsConfigured)
        {
            if (_env.IsDevelopment())
            {
                _logger.LogWarning(
                    "Email not configured — skipping send to {To} ({Subject}). " +
                    "Set Email:TenantId/ClientId/ClientSecret/FromAddress to enable.",
                    to, subject);
                return;
            }

            throw new InvalidOperationException(
                "Email is not configured. Ensure Email:TenantId, Email:ClientId, Email:ClientSecret, " +
                "and Email:FromAddress are set in environment variables (e.g. Email__FromAddress on Railway).");
        }
```

(Production misconfiguration still throws loudly; local dev without creds no longer breaks registration/contact flows.)

- [ ] **Step 5: RegisterConfirmation keys off configuration, not sender type**

In `RegisterConfirmation.razor`:
- Add `@using Microsoft.Extensions.Options` and `@using FellsideDigital.Web.Models`, and `@inject IOptions<EmailSettings> EmailSettings`.
- Replace the branch at line 57:

```csharp
        else if (!EmailSettings.Value.IsConfigured)
        {
            // Email isn't configured in this environment (normal for local dev):
            // render the confirmation link on screen instead.
            var userId = await UserManager.GetUserIdAsync(user);
            var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            emailConfirmationLink = NavigationManager.GetUriWithQueryParameters(
                NavigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri,
                new Dictionary<string, object?> { ["userId"] = userId, ["code"] = code, ["returnUrl"] = ReturnUrl });
        }
```

- Replace the template copy (lines 19-25) with:

```razor
@if (emailConfirmationLink is not null)
{
    <p>
        Email sending isn't configured in this environment, so here is your confirmation
        link directly: <a href="@emailConfirmationLink">Click here to confirm your account</a>
    </p>
}
```

- Remove the now-unused `@inject IEmailSender<ApplicationUser> EmailSender` line and its `@using Microsoft.AspNetCore.Identity` **only if** no other reference in the file needs it (UserManager does need it — keep the using, drop only the EmailSender inject).

- [ ] **Step 6: Delete the dead sender**

```bash
git rm src/FellsideDigital.Web/Components/Account/IdentityNoOpEmailSender.cs
```

Run: `grep -rn "IdentityNoOpEmailSender" src/ CLAUDE.md | grep -v obj/`
Expected: matches only in CLAUDE.md (fixed next step).

- [ ] **Step 7: Fix stale CLAUDE.md notes**

In `CLAUDE.md`:
- Authentication & Roles bullet: replace the sentence about `IdentityNoOpEmailSender` with: `RequireConfirmedAccount = true`; identity emails (confirmation, password reset) are sent for real via `EmailService` (Microsoft Graph). When email is unconfigured (typical local dev), `EmailService` no-ops in Development and `RegisterConfirmation.razor` renders the confirmation link on screen.
- Security "Email is no-op in normal operation" bullet: replace with: **All email — identity and transactional — goes through `IEmailService` (`EmailService`, Microsoft Graph).** Requires `Email:TenantId/ClientId/ClientSecret/FromAddress`; unconfigured + Production throws, unconfigured + Development logs and skips.

- [ ] **Step 8: Build and test**

Run: `dotnet.exe build FellsideDigitalWebsite.sln && dotnet.exe test tests/FellsideDigital.Tests --no-build --filter EmailSettingsTests`
Expected: build succeeds; 5 tests pass.

- [ ] **Step 9: Commit**

```bash
git add -A src/FellsideDigital.Web tests/FellsideDigital.Tests/EmailSettingsTests.cs CLAUDE.md
git commit -m "feat: unify email config handling, drop dead no-op sender"
```

---

### Task 3: UserMenu — correct links per layout + styling fix

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Account/Pages/Manage/UserMenu.razor`
- Modify: `src/FellsideDigital.Web/Components/Layout/PortalLayout.razor:98`
- Modify: `src/FellsideDigital.Web/Components/Layout/AdminLayout.razor:77`

**Interfaces:**
- Produces: `UserMenu` parameter `SettingsUrl` (string?, default null — item hidden when null).

- [ ] **Step 1: Rewrite UserMenu with parameterised settings link and consistent styling**

Replace the `Authorized` content of `UserMenu.razor` (keep the `@code` initials logic, add the parameter). Full replacement file:

```razor
@using FellsideDigital.Web.Components.Account.Pages.Forms
@using Microsoft.AspNetCore.Components.Authorization

<AuthorizeView Context="context">
    <Authorized>
        <div class="relative ml-3">
            <button @onclick="Toggle"
                    class="relative flex max-w-xs items-center rounded-full">

                @if (!string.IsNullOrEmpty(context.User.FindFirst("picture")?.Value))
                {
                    <img src="@context.User.FindFirst("picture")!.Value"
                         class="size-8 rounded-full" />
                }
                else
                {
                    <div class="size-8 rounded-full bg-accent flex items-center justify-center">
                        <span class="text-xs text-white">
                            @GetInitials(context.User.Identity?.Name)
                        </span>
                    </div>
                }
            </button>

            @if (_open)
            {
                <div class="absolute right-0 z-10 mt-2 w-48 rounded-xl bg-white dark:bg-neutral-900
                            border border-gray-200/80 dark:border-white/10 py-1 shadow-lg">

                    <div class="px-4 py-2 border-b border-gray-100 dark:border-white/10">
                        <p class="text-sm font-medium text-gray-900 dark:text-white truncate">@context.User.Identity?.Name</p>
                        <p class="text-xs text-gray-500 dark:text-neutral-400 truncate">@context.User.FindFirst("email")?.Value</p>
                    </div>

                    <a href="/Account/Manage" class="@ItemClass">Your account</a>
                    @if (!string.IsNullOrEmpty(SettingsUrl))
                    {
                        <a href="@SettingsUrl" class="@ItemClass">Settings</a>
                    }

                    <LogoutForm ReturnUrl="@CurrentUrl" />
                </div>
            }
        </div>
    </Authorized>
</AuthorizeView>

@code {
    [Parameter] public string? CurrentUrl { get; set; }

    /// <summary>Optional area-specific settings page; the hosting layout supplies it.</summary>
    [Parameter] public string? SettingsUrl { get; set; }

    private const string ItemClass =
        "block px-4 py-2 text-sm text-gray-700 dark:text-neutral-300 hover:bg-gray-100 dark:hover:bg-white/5";

    private bool _open;

    private void Toggle() => _open = !_open;

    private static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ');
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
            : name[..Math.Min(2, name.Length)].ToUpper();
    }
}
```

Styling deltas from the current file (all defect fixes, no redesign): colourless `border-b` → `border-gray-100 dark:border-white/10`; `dark:bg-gray-800` → `dark:bg-neutral-900` + border to match the app's dropdown panels; explicit text colours on the name/email lines; link classes deduplicated into `ItemClass`. The hard-coded `/portal/settings` link is replaced by the parameter; `/account/manage` stays for everyone (label "Your account").

- [ ] **Step 2: Layouts pass the right target**

`PortalLayout.razor:98`: `<UserMenu />` → `<UserMenu SettingsUrl="/Portal/Settings" />`
`AdminLayout.razor:77`: `<UserMenu />` (unchanged — admins get "Your account" → `/Account/Manage`, which includes password change; no bogus portal link).

- [ ] **Step 3: Build**

Run: `dotnet.exe build FellsideDigitalWebsite.sln`
Expected: success.

- [ ] **Step 4: Commit**

```bash
git add src/FellsideDigital.Web/Components/Account/Pages/Manage/UserMenu.razor src/FellsideDigital.Web/Components/Layout/PortalLayout.razor src/FellsideDigital.Web/Components/Layout/AdminLayout.razor
git commit -m "fix: per-area settings link and consistent styling in user menu"
```

---

### Task 4: Identity Manage pages onto FieldStyles

**Files:**
- Modify: every file in `src/FellsideDigital.Web/Components/Account/Pages/Manage/` that hand-rolls input classes, plus `src/FellsideDigital.Web/Components/Account/Pages/Manage/_Imports.razor`

**Interfaces:**
- Consumes: `FieldStyles.Input`, `FieldStyles.Error` (`FellsideDigital.UI.Components.Forms`).

- [ ] **Step 1: Add the namespace to Manage/_Imports.razor**

Append to `src/FellsideDigital.Web/Components/Account/Pages/Manage/_Imports.razor`:

```razor
@using FellsideDigital.UI.Components.Forms
```

- [ ] **Step 2: Enumerate hand-rolled classes**

Run: `grep -rln "rounded-md bg-white px-3 py-1.5" src/FellsideDigital.Web/Components/Account/Pages/Manage`
Expected: at least `ChangePassword.razor`, `Index.razor`, `Email.razor`, `SetPassword.razor`, `DeletePersonalData.razor` (list may vary — treat the grep output as the authoritative file list).

- [ ] **Step 3: Replace input + validation classes**

In each listed file replace every occurrence of the literal input class string

```
block w-full rounded-md bg-white px-3 py-1.5 text-base text-slate-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-accent sm:text-sm/6 dark:bg-white/5 dark:text-white dark:outline-white/10 dark:placeholder:text-gray-500
```

with `@FieldStyles.Input` (i.e. `class="@FieldStyles.Input"`), and every `ValidationMessage ... class="mt-1 text-xs text-red-600 dark:text-red-400"` with `class="@FieldStyles.Error"`. Where a file uses a *slightly different* literal (e.g. extra `disabled:` utilities), use `@FieldStyles.Extend("...extras only...")` keeping the extras verbatim.

These pages are static SSR forms (`method="post"`); class-constant substitution is safe — do **not** convert them to the interactive `FormField` component in this task.

- [ ] **Step 4: Build + visual sanity**

Run: `dotnet.exe build FellsideDigitalWebsite.sln`
Expected: success.
Run: `grep -rn "rounded-md bg-white px-3 py-1.5" src/FellsideDigital.Web/Components/Account/Pages/Manage`
Expected: no matches.
Note in the task report: inputs change from `rounded-md`/outline style to the app-standard `rounded-xl`/ring style — this is the *intended* convergence with the rest of the app (spec workstream B), the only permitted visual delta in this plan.

- [ ] **Step 5: Commit**

```bash
git add src/FellsideDigital.Web/Components/Account/Pages/Manage
git commit -m "refactor: manage pages use shared FieldStyles"
```

---

### Task 5: Hero iframe reliability (race-proof loader)

**Files:**
- Modify: `src/FellsideDigital.Web/wwwroot/js/hero-carousel.js` (full rewrite below)
- Modify: `src/FellsideDigital.Web/Components/Pages/Marketing/HeroProjectCarousel.razor:86-93` (iframe markup) and `:287-297` (ids + interop)

**Interfaces:**
- Produces: JS API `heroCarousel.tryLoadIframe(iframeId, fallbackId)` (same name/signature as today — interop call site keeps working) plus new inline hook `heroCarousel.onIframeLoad(el)`.

- [ ] **Step 1: Rewrite the JS with a load registry**

Replace `hero-carousel.js` entirely:

```js
window.heroCarousel = {
    // Preview keys (project ids) whose iframe has successfully fired `load`.
    _loaded: new Set(),

    // Wired via the iframe's inline onload attribute, so it is registered
    // before the iframe can possibly finish loading — no race with Blazor's
    // OnAfterRenderAsync interop, which may run after `load` has fired.
    onIframeLoad(iframe) {
        const key = iframe.dataset.previewKey || iframe.id;
        this._loaded.add(key);
        this._swap(iframe, document.getElementById(iframe.dataset.fallbackId), true);
    },

    // Called from Blazor after each render. Idempotent: if this preview
    // already loaded (now or on an earlier visit to this slide), show it
    // immediately; otherwise leave the fallback up and give the site 6s.
    tryLoadIframe(iframeId, fallbackId) {
        const iframe = document.getElementById(iframeId);
        const fallback = document.getElementById(fallbackId);
        if (!iframe) return;

        iframe.dataset.fallbackId = fallbackId;
        const key = iframe.dataset.previewKey || iframeId;

        if (this._loaded.has(key)) {
            this._swap(iframe, fallback, true);
            return;
        }

        this._swap(iframe, fallback, false);

        // Framing refusals (X-Frame-Options / CSP) usually never fire `load`:
        // keep the fallback if nothing arrives in time.
        setTimeout(() => {
            if (!this._loaded.has(key)) this._swap(iframe, fallback, false);
        }, 6000);
    },

    _swap(iframe, fallback, showIframe) {
        iframe.style.display = showIframe ? 'block' : 'none';
        if (fallback) fallback.style.display = showIframe ? 'none' : 'flex';
    }
};
```

- [ ] **Step 2: Update the iframe markup**

In `HeroProjectCarousel.razor` replace the iframe element (lines 86-93) with:

```razor
                            <iframe id="@IframeId"
                                    src="@Current.PreviewUrl"
                                    data-preview-key="@Current.Id"
                                    onload="heroCarousel.onIframeLoad(this)"
                                    sandbox="allow-scripts allow-same-origin"
                                    loading="lazy"
                                    title="@Current.Name preview"
                                    class="absolute inset-0 w-full h-full border-0 bg-white dark:bg-slate-900"
                                    style="display:none;">
                            </iframe>
```

(`onload` is a plain HTML attribute here, not a Blazor event — it works under Interactive Server and fires even when the interop call arrives late. `data-preview-key` keys the registry by project so index reuse of element ids can't cross-contaminate.)

- [ ] **Step 3: Key element ids by project, not index**

In the `@code` block (lines 287-288) change:

```csharp
    private string IframeId => $"hero-iframe-{Current.Id}";
    private string FallbackId => $"hero-fallback-{Current.Id}";
```

(With index-based ids, moving from slide 0→1 reuses different ids for the same DOM position; project-keyed ids make Blazor's diff replace the iframe node outright on slide change, guaranteeing a fresh `load` cycle per project and stable ids for the JS registry.)

- [ ] **Step 4: Build**

Run: `dotnet.exe build FellsideDigitalWebsite.sln`
Expected: success.

- [ ] **Step 5: Runtime verification (required before claiming done)**

This is the user-visible bug; verify in a browser (VS Docker on :8080, or `dotnet run --launch-profile http` if DB available):
1. Load `/` — website project preview should swap from wireframe/screenshot to live iframe within a few seconds.
2. Click Next then Prev — returning to the first project must show the iframe **immediately** (no fallback flash, no 6s wait).
3. Click the same dot repeatedly / trigger re-renders — iframe must stay visible.
If the environment can't run the app (no DB/Docker), state that explicitly in the task report and flag manual verification for Oliver — do not claim verified.

- [ ] **Step 6: Commit**

```bash
git add src/FellsideDigital.Web/wwwroot/js/hero-carousel.js src/FellsideDigital.Web/Components/Pages/Marketing/HeroProjectCarousel.razor
git commit -m "fix: race-proof hero project iframe loading"
```

---

### Task 6: `FieldStyles.MarketingInput` + converge Contact/Scan

**Files:**
- Modify: `src/FellsideDigital.UI/Components/Forms/FieldStyles.cs`
- Modify: `src/FellsideDigital.Web/Components/Pages/Marketing/Contact.razor.cs:77-83` (delete local const), `Contact.razor` (class references)
- Modify: `src/FellsideDigital.Web/Components/Pages/Marketing/Scan.razor.cs:39-45` (delete local const), `Scan.razor` (class references)

**Interfaces:**
- Produces: `FieldStyles.MarketingInput` (string const) — the marketing-page input look.

- [ ] **Step 1: Add the constant**

In `FieldStyles.cs` after `TextArea`:

```csharp
    /// <summary>
    /// Marketing-page input styling (Contact, Scan): slightly larger padding and a
    /// softer accent focus ring than the admin/portal <see cref="Input"/>.
    /// </summary>
    public const string MarketingInput =
        "w-full rounded-xl px-4 py-2.5 text-sm " +
        "bg-slate-50 dark:bg-neutral-800 " +
        "ring-1 ring-slate-200 dark:ring-white/10 " +
        "text-slate-900 dark:text-neutral-100 " +
        "placeholder:text-slate-400 dark:placeholder:text-neutral-500 " +
        "focus:outline-none focus:ring-2 focus:ring-accent/50 transition";
```

(This is Contact's existing string verbatim. Scan's variant — `bg-white/5` + `border` instead of `bg-neutral-800` + `ring` — converges onto it; near-identical rendering, and convergence is the point.)

- [ ] **Step 2: Point both pages at it**

- `Contact.razor.cs`: delete the `private const string InputClass = ...` block (lines 77-83); in `Contact.razor` replace `@InputClass` with `@FieldStyles.MarketingInput` (add `@using FellsideDigital.UI.Components.Forms` at the top if the page-level `_Imports.razor` doesn't already cover it — check with `grep -n "UI.Components.Forms" src/FellsideDigital.Web/Components/_Imports.razor`, which is expected to already include it).
- `Scan.razor.cs`: same — delete its `InputClass` const (lines 39-45), replace `@InputClass` usages in `Scan.razor` with `@FieldStyles.MarketingInput`. Leave `ToggleClass` alone (it's a real page-specific style).

- [ ] **Step 3: Build + verify no local marketing input consts remain**

Run: `dotnet.exe build FellsideDigitalWebsite.sln && grep -rn "private const string InputClass" src/FellsideDigital.Web/Components/Pages/Marketing`
Expected: build succeeds; grep finds nothing.

- [ ] **Step 4: Commit**

```bash
git add src/FellsideDigital.UI/Components/Forms/FieldStyles.cs src/FellsideDigital.Web/Components/Pages/Marketing
git commit -m "refactor: shared MarketingInput style for contact and scan forms"
```

---

### Task 7: Deduplicate Documents/Notes input strings

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Documents.razor.cs:27-30`
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Notes.razor.cs:31-34`

- [ ] **Step 1: Replace the duplicated literals**

Both files currently re-declare `FieldStyles.Input`'s exact string. Replace each block with:

```csharp
    private const string InputClass = FellsideDigital.UI.Components.Forms.FieldStyles.Input;
```

(This matches the aliasing pattern already used by `Edit.razor.cs`, `Create.razor.cs`, etc.)

- [ ] **Step 2: Build + verify**

Run: `dotnet.exe build FellsideDigitalWebsite.sln && grep -rn "ring-1 ring-inset ring-gray-200" src/FellsideDigital.Web/Components/Pages --include="*.cs"`
Expected: build succeeds; grep finds nothing (the literal lives only in `FieldStyles.cs`).

- [ ] **Step 3: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/Documents.razor.cs src/FellsideDigital.Web/Components/Pages/Admin/Projects/Notes.razor.cs
git commit -m "refactor: reuse FieldStyles.Input in documents and notes pages"
```

---

### Task 8: MultiProjectOverview onto Th/Td/TableStyles

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Portal/Overview/MultiProjectOverview.razor:136-170` (table region)

**Interfaces:**
- Consumes: `Th`, `Td`, `TableStyles` from `FellsideDigital.UI.Components.Tables` (already in `_Imports.razor` per CLAUDE.md conventions — verify with `grep -n "UI.Components.Tables" src/FellsideDigital.Web/Components/_Imports.razor`).

- [ ] **Step 1: Read the current table block and the component contracts**

Read `src/FellsideDigital.UI/Components/Tables/Th.razor`, `Td.razor`, `TableStyles.cs`, then `MultiProjectOverview.razor:130-200`. Map: `<table class="w-full border-collapse">` → `TableStyles.Table` (or keep the literal if TableStyles.Table differs — visual parity wins); each `<th class="text-left px-5 py-3 text-[11px] font-semibold uppercase ...">Label</th>` → `<Th>Label</Th>`; each `<td class="px-5 py-3.5">…</td>` → `<Td>…</Td>` (pass any *extra* classes via the component's class parameter if one exists — check the component source for the parameter name before writing).

- [ ] **Step 2: Apply the conversion**

Rewrite lines 136-170 using the components. Preserve every non-table utility class exactly (cell content markup is untouched). If `Th`/`Td` defaults differ from this table's `px-5 py-3` paddings, pass the page's paddings through the component's class/extra parameter rather than accepting different spacing — **visual parity is the acceptance bar**.

- [ ] **Step 3: Build + verify no hand-rolled th remains**

Run: `dotnet.exe build FellsideDigitalWebsite.sln && grep -rln "<th " src/FellsideDigital.Web/Components --include="*.razor" | grep -v obj/`
Expected: build succeeds; no files listed.

- [ ] **Step 4: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Portal/Overview/MultiProjectOverview.razor
git commit -m "refactor: portal overview table uses shared Th/Td components"
```

---

### Task 9: Portal Settings + interactive forms onto FormField

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Portal/Settings.razor` (both form sections)
- Possibly modify (same pattern, check first): `src/FellsideDigital.Web/Components/Pages/Portal/Testimonial.razor`

**Interfaces:**
- Consumes: `FormField` (`FellsideDigital.UI.Components.Forms`). Read `src/FellsideDigital.UI/Components/Forms/FormField.razor` FIRST to get its exact parameters (label text, required flag, hint, validation `For` expression, child content slot) — use whatever the component actually exposes; do not guess parameter names.

- [ ] **Step 1: Read FormField.razor and one existing usage**

Run: `grep -rln "<FormField" src/FellsideDigital.Web/Components --include="*.razor" | grep -v obj/ | head -3` and read one hit to copy the established usage idiom exactly.

- [ ] **Step 2: Convert Settings.razor**

Replace each hand-rolled `label + InputText + ValidationMessage` block (six of them: FirstName, LastName across the profile form; Current/New/Confirm password in the password form — the disabled email display stays as-is since it's not an input field pattern FormField covers, unless FormField supports a disabled/readonly slot per Step 1's reading) with the FormField idiom found in Step 1, binding the same `ProfileInput.*`/`PasswordInput.*` properties and keeping `type="password"` on the password inputs. Delete the page's local `InputClass` alias in `Settings.razor.cs:29` once nothing references it.

- [ ] **Step 3: Check Testimonial.razor for the same pattern**

Run: `grep -n "label class=\"block text-sm" src/FellsideDigital.Web/Components/Pages/Portal/Testimonial.razor`
If it hand-rolls the same standard blocks, convert them identically; if its layout is bespoke (e.g. star ratings), leave it and note why in the commit message.

- [ ] **Step 4: Build**

Run: `dotnet.exe build FellsideDigitalWebsite.sln`
Expected: success.

- [ ] **Step 5: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Portal
git commit -m "refactor: portal forms use shared FormField component"
```

---

### Task 10: New shared components — extract only what genuinely repeats

**Files:**
- Potentially create: `src/FellsideDigital.UI/Components/Navigation/Breadcrumb.razor`, `src/FellsideDigital.UI/Components/Cards/BrowserFrame.razor`, `src/FellsideDigital.UI/Components/Cards/MetricTile.razor`
- Modify: usage sites identified below; `src/FellsideDigital.Web/Components/_Imports.razor` and the CLAUDE.md component table for anything actually created.

**Decision rule (applies to every candidate):** run the stated grep; extract the component **only if there are ≥2 real usage sites**. If only 1, skip it and record "skipped (single use, YAGNI)" in the commit message. Never restyle while extracting — the component must emit the exact classes the first usage site has today, with the second site's deltas as parameters.

- [ ] **Step 1: Breadcrumb candidate**

Run: `grep -rln "aria-label=\"Breadcrumb\"" src/FellsideDigital.Web/Components --include="*.razor" | grep -v obj/`
If ≥2 sites, create `src/FellsideDigital.UI/Components/Navigation/Breadcrumb.razor`:

```razor
@* Marketing breadcrumb trail. Items render as link|separator; last item is plain text. *@
<nav aria-label="Breadcrumb"
     class="mb-8 flex items-center gap-1.5 text-xs font-medium text-slate-400 dark:text-neutral-500">
    @for (var i = 0; i < Items.Count; i++)
    {
        var item = Items[i];
        if (i > 0)
        {
            <span>/</span>
        }
        @if (item.Href is not null && i < Items.Count - 1)
        {
            <a href="@item.Href" class="hover:text-accent transition-colors">@item.Label</a>
        }
        else
        {
            <span class="text-slate-600 dark:text-neutral-300">@item.Label</span>
        }
    }
</nav>

@code {
    public readonly record struct Crumb(string Label, string? Href);

    [Parameter, EditorRequired] public IReadOnlyList<Crumb> Items { get; set; } = [];
}
```

and replace the hand-rolled `<nav aria-label="Breadcrumb">` blocks at the usage sites with `<Breadcrumb Items="..." />` passing the same labels/hrefs.

- [ ] **Step 2: BrowserFrame candidate (browser-chrome preview shell)**

Run: `grep -rln "rounded-full bg-red-400" src/FellsideDigital.Web/Components --include="*.razor" | grep -v obj/`
(The three traffic-light dots are the fingerprint.) If ≥2 sites (`HeroProjectCarousel.razor` + e.g. `Websites.razor`), extract the chrome bar + viewport shell into `src/FellsideDigital.UI/Components/Cards/BrowserFrame.razor` with parameters `Url` (string?, shown in the address pill), `ExternalHref` (string?, renders the open-in-new-tab icon link when set), and `ChildContent` (the viewport). Emit exactly the classes currently in `HeroProjectCarousel.razor:55-80`. If only the hero uses it, skip.

- [ ] **Step 3: MetricTile candidate**

Run: `grep -rn "text-base sm:text-xl font-bold leading-none" src/FellsideDigital.Web/Components --include="*.razor" | grep -v obj/`
If the hero metric tile markup (icon + value + label, `HeroProjectCarousel.razor:243-250`) appears at ≥2 sites, extract `MetricTile.razor` with parameters `Icon` (IconName), `IconClass`, `Value`, `ValueClass`, `Label`. Otherwise skip.

- [ ] **Step 4: Register + document whatever was created**

For each created component: confirm its namespace has a `@using` in `src/FellsideDigital.Web/Components/_Imports.razor` (add if new), and add a row to the CLAUDE.md "UI component library first" table.

- [ ] **Step 5: Build + commit**

Run: `dotnet.exe build FellsideDigitalWebsite.sln`
Expected: success.

```bash
git add src/FellsideDigital.UI src/FellsideDigital.Web/Components CLAUDE.md
git commit -m "refactor: extract repeated markup into shared UI components"
```

(Commit message must list what was extracted and what was skipped with reasons.)

---

### Task 11: Admin & portal page-by-page defect pass

**Files:**
- Potentially modify: any file under `src/FellsideDigital.Web/Components/Pages/Admin/` and `.../Pages/Portal/`

**Procedure — run each check below over every page in both areas; fix what fails, leave what passes. This task fixes defects against existing conventions; it does not restyle.**

- [ ] **Step 1: Dark-mode coverage check**

Run: `grep -rn 'class="[^"]*bg-white[^"]*"' src/FellsideDigital.Web/Components/Pages/Admin src/FellsideDigital.Web/Components/Pages/Portal --include="*.razor" | grep -v "dark:" | grep -v obj/`
Each hit is a light-only background — add the `dark:` variant used by sibling elements on the same page (match the page's existing dark palette: `dark:bg-neutral-900` for panels, `dark:bg-white/5` for inset fields). Repeat the grep for `text-gray-900` and `border-gray-200` without `dark:`.

- [ ] **Step 2: Empty-state check**

Run: `grep -rn "Count == 0\|!.*\.Any()" src/FellsideDigital.Web/Components/Pages/Admin src/FellsideDigital.Web/Components/Pages/Portal --include="*.razor" | grep -v obj/`
For each empty-branch that renders a bare `<p>`/`<div>` message instead of `<EmptyState ...>`, convert it to `EmptyState` (read `src/FellsideDigital.UI/Components/Feedback/EmptyState.razor` for its parameters first, and copy an existing usage: `grep -rln "<EmptyState" src/FellsideDigital.Web/Components --include="*.razor" | head -1`).

- [ ] **Step 3: Feedback-convention check**

For every page with a mutating action (save/delete/status change), confirm the code-behind (a) wraps the risky call in try/catch with `ErrorHandling.LogAndDescribe`, and (b) reports the outcome via `ToastService` (transient outcomes) or `AlertBanner` (inline form errors). Run: `grep -rLn "LogAndDescribe" $(grep -rln "SaveAsync\|DeleteAsync\|UpdateAsync" src/FellsideDigital.Web/Components/Pages/Admin src/FellsideDigital.Web/Components/Pages/Portal --include="*.razor.cs" | grep -v obj/)` — audit each returned file individually (some may legitimately delegate error handling; only fix genuine gaps).

- [ ] **Step 4: Responsive table check**

Run: `grep -rn "<table" src/FellsideDigital.Web/Components/Pages/Admin src/FellsideDigital.Web/Components/Pages/Portal --include="*.razor" | grep -v obj/`
Every table must sit inside a horizontally scrollable wrapper (`overflow-x-auto`). Add the wrapper where missing.

- [ ] **Step 5: Build, list what changed, commit**

Run: `dotnet.exe build FellsideDigitalWebsite.sln`
Expected: success.
The task report must enumerate every page touched and the specific defect fixed (file:line → what/why).

```bash
git add src/FellsideDigital.Web/Components/Pages
git commit -m "fix: dark-mode, empty-state, feedback and responsive defects across portals"
```

---

### Task 12: SEO — default og:image, sitemap bump, JSON-LD tests, owner checklist

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Shared/SeoHead.razor`
- Modify: `src/FellsideDigital.Web/wwwroot/sitemap.xml`
- Create: `tests/FellsideDigital.Tests/LocationSeoTests.cs`
- Create: `docs/seo/owner-actions.md`

- [ ] **Step 1: Write failing JSON-LD tests**

`tests/FellsideDigital.Tests/LocationSeoTests.cs` (adjust the static class/type names to whatever `Locations.razor.model.cs` actually declares — read it first; the model holds 4 locations: keswick, penrith, kendal, carlisle):

```csharp
using System.Text.Json;
using FellsideDigital.Web.Components.Pages.Marketing;

namespace FellsideDigital.Tests;

public class LocationSeoTests
{
    public static TheoryData<string> Slugs() => new("keswick", "penrith", "kendal", "carlisle");

    [Theory]
    [MemberData(nameof(Slugs))]
    public void Every_location_emits_parseable_json_ld(string slug)
    {
        var loc = LocationData.All.Single(l => l.Slug == slug);

        foreach (var json in new[]
                 {
                     LocationData.ServiceJson(loc),
                     LocationData.BreadcrumbJson(loc),
                     LocationData.FaqJson(loc),
                 })
        {
            using var doc = JsonDocument.Parse(json); // throws on invalid JSON
            Assert.True(doc.RootElement.TryGetProperty("@type", out _));
        }
    }
}
```

(If `LocationData.All` / method names differ, use the real members — the test's intent is: every location × every JSON-LD builder → parseable JSON with an `@type`.)

- [ ] **Step 2: Run, adjust names until it compiles, verify it passes**

Run: `dotnet.exe build FellsideDigitalWebsite.sln && dotnet.exe test tests/FellsideDigital.Tests --no-build --filter LocationSeoTests`
Expected: 4 tests pass. (If a JSON builder emits invalid JSON, that's a real found bug — fix the builder, not the test.)

- [ ] **Step 3: Default og:image in SeoHead**

In `SeoHead.razor` replace both `@if (!string.IsNullOrEmpty(OgImage))` blocks with unconditional tags, and default the parameter:

```razor
    <meta property="og:image" content="@ResolvedOgImage" />
```
```razor
    <meta name="twitter:image" content="@ResolvedOgImage" />
```
```csharp
    [Parameter] public string? OgImage { get; set; }

    private string ResolvedOgImage =>
        string.IsNullOrEmpty(OgImage)
            ? "https://fellsidedigital.co.uk/web-app-manifest-512x512.png"
            : OgImage;
```

- [ ] **Step 4: Sitemap parity + lastmod bump**

- Verify parity: public marketing routes are `/`, `/websites`, `/automation`, `/contact`, `/website-design/{keswick,penrith,kendal,carlisle}` (`/scan` deliberately excluded, matches robots.txt disallow). Sitemap already lists exactly these 8 — confirm nothing was added/removed by this branch.
- Update every `<lastmod>2026-06-18</lastmod>` → `<lastmod>2026-07-05</lastmod>`.

- [ ] **Step 5: Owner checklist**

Create `docs/seo/owner-actions.md`:

```markdown
# SEO — owner actions (outside code)

Verified in code on 2026-07-05: meta/canonical/OG on all marketing pages,
JSON-LD (LocalBusiness, Service, Breadcrumb, FAQ) valid, sitemap ↔ route parity,
robots.txt correct, llms.txt present, default og:image set.

Things only the site owner can do:

1. **Google Search Console** — verify the property and paste the verification
   token into `src/FellsideDigital.Web/Components/App.razor` (the commented-out
   `google-site-verification` meta, ~line 30). Then submit `sitemap.xml`.
2. **Google Business Profile** — create/claim the listing for Fellside Digital
   (Keswick, Cumbria); it feeds the local-pack results the
   `/website-design/{town}` pages target.
3. **Bing Webmaster Tools** — import from Search Console once (1) is done.
4. After any future page addition: add it to `wwwroot/sitemap.xml` and bump
   `<lastmod>`.
```

- [ ] **Step 6: Full build + full test suite**

Run: `dotnet.exe build FellsideDigitalWebsite.sln && dotnet.exe test tests/FellsideDigital.Tests --no-build`
Expected: build succeeds; all tests pass (DB-backed tests require Docker — if Docker is down, run with `--filter "EmailSettingsTests|LocationSeoTests|ToastServiceTests|ErrorHandlingTests|EmailTemplateTests"` and note the skip).

- [ ] **Step 7: Commit**

```bash
git add src/FellsideDigital.Web/Components/Shared/SeoHead.razor src/FellsideDigital.Web/wwwroot/sitemap.xml tests/FellsideDigital.Tests/LocationSeoTests.cs docs/seo/owner-actions.md
git commit -m "feat: default og:image, sitemap bump, JSON-LD tests, SEO owner checklist"
```

---

## Final verification (after all tasks)

- [ ] `dotnet.exe build FellsideDigitalWebsite.sln` — clean.
- [ ] `dotnet.exe test tests/FellsideDigital.Tests --no-build` — green (note any Docker-skipped collections).
- [ ] `grep -rn "IdentityNoOpEmailSender" src/ | grep -v obj/` — empty.
- [ ] `grep -rn "private const string InputClass =$" -A1 src/FellsideDigital.Web --include="*.cs" | grep -v "FieldStyles"` — no raw literals.
- [ ] Oliver: VS Docker rebuild → visual pass on `/` (iframes), `/Portal/*`, `/Admin/*`, `/Account/Manage/*` in light + dark mode.
- [ ] Password flows verified: change password via `/Portal/Settings` (client) and `/Account/Manage/ChangePassword` (admin, reached from the user menu); forgot-password issues a Graph email in an email-configured environment (or logs the dev skip locally).
- [ ] Hand over `docs/seo/owner-actions.md`.
