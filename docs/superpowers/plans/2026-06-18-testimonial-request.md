# Testimonial Request Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let an admin copy a link to the testimonial form and, once a project is Completed, email the client a request to leave a testimonial.

**Architecture:** Two affordances on the admin project Detail page's Client card. The copy button uses the existing clipboard JS-interop pattern; the email button calls a new `EmailService.SendTestimonialRequestAsync` backed by a new `EmailTemplates.TestimonialRequest`. Both point clients at the existing login-gated `/Portal/Testimonial` form. No schema changes.

**Tech Stack:** .NET 10, Blazor Server (Interactive Server), Microsoft Graph email, xUnit. Spec: `docs/superpowers/specs/2026-06-18-testimonial-request-design.md`.

## Global Constraints

- Error handling (`CLAUDE.md`): wrap every action in `try/catch`; never surface `ex.Message`; log via `ErrorHandling.LogAndDescribe(Logger, ex, "…")`; report outcomes with `ToastService` (`Toasts.Success/Error`).
- Reuse UI/service patterns; do not inject `FellsideDigitalDbContext` into components.
- `EmailTemplates`/`EmailTheme` are `internal` and exposed to tests via `InternalsVisibleTo("FellsideDigital.Tests")` in `FellsideDigital.Web.csproj`. Never hardcode colours or button markup in templates — use `EmailTheme` helpers (`H2`, `P`, `Greeting`, `EmailTheme.Button`, `EmailTheme.Layout`).
- The testimonial URL is always `NavigationManager.ToAbsoluteUri("/Portal/Testimonial").ToString()` — the same login-gated page for every client.
- WSL build note (project memory): build/test via `dotnet.exe`; scope EF to the Web csproj. Commands below are written as `dotnet` — use `dotnet.exe` under WSL.

---

### Task 1: Testimonial request email (template + sender + tests)

**Files:**
- Modify: `src/FellsideDigital.Web/Services/Email/EmailTemplates.cs` (add `TestimonialRequest`, in the "Portal activity notifications" section ~line 181)
- Modify: `src/FellsideDigital.Web/Services/EmailService.cs` (add `SendTestimonialRequestAsync`, in the "Portal activity notifications" section ~line 77-105)
- Test: `tests/FellsideDigital.Tests/EmailTemplateTests.cs` (add a focused test + add to the `AllTemplates` MemberData)

**Interfaces:**
- Consumes: `EmailTheme.Layout`, `EmailTheme.Button`, private `H2`, `P`, `Greeting` (all in `EmailTemplates`); `ApplicationUser` (has `FirstName`, `Email`), `ClientProject` (has `Name`); `EmailService.SendAsync(string to, string subject, string htmlBody, bool bccAdmin = false)`.
- Produces:
  - `EmailTemplates.TestimonialRequest(ApplicationUser client, ClientProject project, string testimonialUrl)` → `string` (HTML body)
  - `EmailService.SendTestimonialRequestAsync(ApplicationUser client, ClientProject project, string testimonialUrl)` → `Task`

- [ ] **Step 1: Write the failing test**

In `tests/FellsideDigital.Tests/EmailTemplateTests.cs`, add this test (place it after `DocumentAdded_includes_title_project_and_cta`):

```csharp
    [Fact]
    public void TestimonialRequest_includes_project_name_cta_and_greeting()
    {
        var project = Project();
        var url = "https://fellsidedigital.co.uk/Portal/Testimonial";

        var html = EmailTemplates.TestimonialRequest(Client(), project, url);

        Assert.Contains(project.Name, html);                 // "Acme Rebuild"
        Assert.Contains(url, html);                           // CTA points at the form
        Assert.Contains("Ada", html);                         // greeting uses first name
        Assert.Contains("testimonial", html, StringComparison.OrdinalIgnoreCase);
    }
```

Also add the new template to the existing `AllTemplates()` MemberData so the branded/blue guard covers it. Inside `AllTemplates()`, after the `InvoiceStatusChanged` line (~line 93), add:

```csharp
        yield return [EmailTemplates.TestimonialRequest(Client(), Project(), url)];
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~EmailTemplateTests"`
Expected: FAIL to **compile** with "'EmailTemplates' does not contain a definition for 'TestimonialRequest'".

- [ ] **Step 3: Add the template**

In `src/FellsideDigital.Web/Services/Email/EmailTemplates.cs`, under the `// ── Portal activity notifications ──` header (after `DocumentAdded`, ~line 192), add:

```csharp
    public static string TestimonialRequest(ApplicationUser client, ClientProject project, string testimonialUrl) =>
        EmailTheme.Layout($"""
            {H2("How did we do?")}
            {P($"{Greeting(client.FirstName)} now that your <strong>{project.Name}</strong> project is complete, we'd love to hear how it went. A short testimonial helps other businesses know what it's like to work with us — it only takes a minute.")}
            {P("Log in to your portal using the button below and share a few words. Already left one? The same link lets you update it.")}
            <div style="margin:0 0 4px;">{EmailTheme.Button(testimonialUrl, "Leave a testimonial →")}</div>
            """);
```

- [ ] **Step 4: Add the sender method**

In `src/FellsideDigital.Web/Services/EmailService.cs`, under the `// ── Portal activity notifications ──` header (after `SendInvoiceStatusChangedAsync`, ~line 105), add:

```csharp
    public Task SendTestimonialRequestAsync(ApplicationUser client, ClientProject project, string testimonialUrl) =>
        SendAsync(
            client.Email!,
            $"How did your {project.Name} project go?",
            EmailTemplates.TestimonialRequest(client, project, testimonialUrl),
            bccAdmin: true);
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~EmailTemplateTests"`
Expected: PASS (all EmailTemplateTests, including the new test and the `Every_template_is_blue_and_branded` theory row for `TestimonialRequest`).

- [ ] **Step 6: Commit**

```bash
git add src/FellsideDigital.Web/Services/Email/EmailTemplates.cs \
        src/FellsideDigital.Web/Services/EmailService.cs \
        tests/FellsideDigital.Tests/EmailTemplateTests.cs
git commit -m "feat: add testimonial request email template and sender"
```

---

### Task 2: Detail page — copy link + request button

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor.cs` (injections, fields, load logic, two methods)
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor` (Client card buttons)

**Interfaces:**
- Consumes: `EmailService.SendTestimonialRequestAsync(ApplicationUser, ClientProject, string)` (Task 1); `ITestimonialService.GetForUserAsync(string userId)` → `Task<ClientTestimonial?>`; `NavigationManager.ToAbsoluteUri`; `IJSRuntime.InvokeVoidAsync`; `ToastService`; `ErrorHandling.LogAndDescribe`; `SpinnerButton` (`OnClick` EventCallback, `Type`, `LoadingText`, `IsLoading`).
- Produces: nothing consumed by later tasks (final task).

> **Note on testing:** this codebase has no Blazor component-test harness (xUnit + Testcontainers only), and there are no existing component tests for admin pages. This task is verified by a clean build plus the manual checklist in Step 7 — consistent with the established pattern. Do not scaffold a new test framework.

- [ ] **Step 1: Add injections and fields to the code-behind**

In `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor.cs`:

Add `using Microsoft.JSInterop;` to the using block at the top.

After the existing `[Inject] private ILogger<Detail> Logger ...` line (~line 18), add:

```csharp
    [Inject] private ITestimonialService Testimonials { get; set; } = default!;
    [Inject] private EmailService Email { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
```

After the existing `private bool _deleting;` field (~line 24), add:

```csharp
    private bool _clientHasTestimonial;
    private bool _sendingRequest;
```

- [ ] **Step 2: Load whether the client already has a testimonial**

In the same file, in `LoadAsync()`, immediately after `_project = await ProjectService.GetByIdAsync(Id);` (~line 105), add:

```csharp
        _clientHasTestimonial = _project is not null
            && await Testimonials.GetForUserAsync(_project.ClientId) is not null;
```

- [ ] **Step 3: Add the copy and request methods**

In the same file, after `ConfirmDeleteAsync()` (before the closing brace of the class, ~line 132), add:

```csharp
    private async Task CopyTestimonialLinkAsync()
    {
        var url = NavigationManager.ToAbsoluteUri("/Portal/Testimonial").ToString();
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", url);
            Toasts.Success("Testimonial link copied to clipboard.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "copying the testimonial link"));
        }
    }

    private async Task RequestTestimonialAsync()
    {
        if (_project?.Client?.Email is not { Length: > 0 })
        {
            Toasts.Error("This client has no email address on file.");
            return;
        }

        _sendingRequest = true;
        try
        {
            var url = NavigationManager.ToAbsoluteUri("/Portal/Testimonial").ToString();
            await Email.SendTestimonialRequestAsync(_project.Client, _project, url);
            _clientHasTestimonial = true;
            Toasts.Success($"Testimonial request sent to {_project.Client.Email}.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "sending the testimonial request"));
        }
        finally
        {
            _sendingRequest = false;
        }
    }
```

(Setting `_clientHasTestimonial = true` after a successful send swaps the button for the "already requested/left" note so the admin doesn't double-send in the same view.)

- [ ] **Step 4: Add the buttons to the Client card**

In `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor`, find the existing "Email client" block inside the Client card (~lines 180-189):

```razor
                    @if (!string.IsNullOrWhiteSpace(_project.Client?.Email))
                    {
                        <a href="mailto:@_project.Client!.Email"
                           class="mt-4 inline-flex items-center gap-1.5 text-sm font-semibold text-accent hover:opacity-80 transition-opacity">
                            Email client
                            <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                            </svg>
                        </a>
                    }
```

Immediately **after** that `@if` block (still inside the `<div class="px-6 py-5">` of the Client card), add:

```razor
                    <div class="mt-4 flex flex-col items-start gap-2.5 border-t border-gray-100 dark:border-white/5 pt-4">
                        <button type="button" @onclick="CopyTestimonialLinkAsync"
                                class="inline-flex items-center gap-1.5 text-sm font-semibold text-accent hover:opacity-80 transition-opacity">
                            <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 17.25v3.375c0 .621-.504 1.125-1.125 1.125h-9.75a1.125 1.125 0 0 1-1.125-1.125V7.875c0-.621.504-1.125 1.125-1.125H6.75a9.06 9.06 0 0 1 1.5.124m7.5 10.376h3.375c.621 0 1.125-.504 1.125-1.125V11.25c0-4.46-3.243-8.161-7.5-8.876a9.06 9.06 0 0 0-1.5-.124H9.375c-.621 0-1.125.504-1.125 1.125v3.5m7.5 10.375H9.375a1.125 1.125 0 0 1-1.125-1.125v-9.25m12 6.625v-1.875a3.375 3.375 0 0 0-3.375-3.375h-1.5a1.125 1.125 0 0 1-1.125-1.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H9.75" />
                            </svg>
                            Copy testimonial link
                        </button>

                        @if (_project.Status == ProjectStatus.Completed)
                        {
                            @if (_clientHasTestimonial)
                            {
                                <p class="text-xs text-gray-400 dark:text-neutral-500">This client has already left a testimonial.</p>
                            }
                            else
                            {
                                <SpinnerButton Type="button"
                                               Color="ButtonColor.Muted"
                                               Style="ButtonStyle.Outline"
                                               IsLoading="_sendingRequest"
                                               LoadingText="Sending…"
                                               OnClick="RequestTestimonialAsync">
                                    Request a testimonial
                                </SpinnerButton>
                            }
                        }
                    </div>
```

(`ProjectStatus` and `ButtonColor`/`ButtonStyle` are already in scope: `Detail.razor` has `@using FellsideDigital.Domain.Enums`, and `SpinnerButton`/`ButtonColor` resolve through `_Imports.razor`.)

- [ ] **Step 5: Build to verify it compiles**

Run: `dotnet build src/FellsideDigital.Web`
Expected: Build succeeded. (Ignore the known flaky `App.razor` `Html` CS0103 generator artifact per project memory if it appears; a clean rebuild clears it.)

- [ ] **Step 6: Run the full test suite to confirm no regressions**

Run: `dotnet test tests/FellsideDigital.Tests --filter "FullyQualifiedName~EmailTemplateTests|FullyQualifiedName~ToastServiceTests|FullyQualifiedName~ErrorHandlingTests"`
Expected: PASS (pure-logic tests; no Docker needed). The Testcontainers-backed tests require Docker and are out of scope for this change.

- [ ] **Step 7: Manual verification checklist**

Run the app (`dotnet run --launch-profile http`, or the VS Docker stack on :8080 per project memory) and, as a SiteAdmin, open a project's Detail page:
  1. **Copy link** is visible on the Client card for any project; clicking it shows a success toast and the clipboard holds the absolute `/Portal/Testimonial` URL.
  2. For a project **not** Completed, no "Request a testimonial" button shows.
  3. For a **Completed** project whose client has **no** testimonial, the "Request a testimonial" button shows; clicking it shows "Testimonial request sent to …" and the button is replaced by the "already left a testimonial" note. (With email env vars unset, expect the friendly "Email is not configured" error toast instead — confirms the catch path, not a bug.)
  4. For a **Completed** project whose client **has** a testimonial, the note shows instead of the button.

- [ ] **Step 8: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor.cs
git commit -m "feat: copy testimonial link and request-on-complete email on project detail"
```

---

## Self-Review

**Spec coverage:**
- Copy testimonial link on Detail Client card → Task 2 (Steps 3–4, copy button).
- Login-required `/Portal/Testimonial` URL → Global Constraints + Task 2 methods.
- Request email only when Completed + hidden when already submitted → Task 2 (Step 2 load, Step 4 conditional render).
- `SendTestimonialRequestAsync` + `TestimonialRequest` template, BCC admin, branded → Task 1.
- Error handling via `LogAndDescribe` + toasts → both tasks.
- Pure-logic template unit test → Task 1 (Steps 1–5).
- Out-of-scope items (tokenised links, auto-send, DB flag, update feature, admin editing) → none added. ✓

**Placeholder scan:** No TBD/TODO; all code blocks complete. ✓

**Type consistency:** `GetForUserAsync(string)` ↔ `ClientProject.ClientId` is `string` ✓; `SendTestimonialRequestAsync(ApplicationUser, ClientProject, string)` identical in Task 1 (produced) and Task 2 (consumed) ✓; `SpinnerButton` params (`Type`, `Color`, `Style`, `IsLoading`, `LoadingText`, `OnClick`) match the component ✓.
