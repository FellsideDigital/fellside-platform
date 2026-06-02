using FellsideDigital.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FellsideDigital.Web.Data.Seeding;

/// <summary>
/// Seeds two demo clients and four showcase projects (two websites, two automations) so the
/// marketing hero carousel has real content in development. Idempotent and dev-only — see
/// <see cref="Extensions.StartupCompositionExtensions.ApplyStartupTasksAsync"/>.
/// </summary>
public static class DemoDataSeeder
{
    // Must satisfy the Identity password policy (length >= 12, upper, lower, digit, non-alphanumeric).
    private const string DemoPassword = "DemoClient!2026";

    public static async Task SeedDemoProjectsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FellsideDigitalDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DemoDataSeeder));

        // Idempotency guard: never duplicate, never touch an environment that already has projects.
        if (await db.ClientProjects.AnyAsync())
        {
            return;
        }

        // CreatedByAdminId is a required FK — find the seeded admin to own these projects.
        var admin = await ResolveAdminAsync(db, userManager);
        if (admin is null)
        {
            logger.LogWarning("DemoDataSeeder skipped: no admin user found to own demo projects.");
            return;
        }

        var harbourline = await EnsureClientAsync(userManager, logger,
            email: "harbourline@demo.fellside.digital",
            firstName: "Sam", lastName: "Marsh", company: "Harbourline Coffee Co.");

        var pennine = await EnsureClientAsync(userManager, logger,
            email: "pennine@demo.fellside.digital",
            firstName: "Dale", lastName: "Hartley", company: "Pennine Plant Hire");

        if (harbourline is null || pennine is null)
        {
            logger.LogWarning("DemoDataSeeder skipped: failed to create one or more demo client accounts.");
            return;
        }

        var now = DateTime.UtcNow;

        var projects = new List<ClientProject>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Harbourline Storefront",
                Description = "Headless e-commerce storefront for a local coffee roastery with same-day delivery scheduling.",
                Type = ProjectType.Website,
                Status = ProjectStatus.Completed,
                Progress = 100,
                ClientId = harbourline.Id,
                CreatedByAdminId = admin.Id,
                IsHeroProject = true,
                HeroDisplayOrder = 0,
                HeroTagline = "Headless storefront with same-day local delivery",
                // Demo: a real frame-friendly site so the live iframe preview works.
                // In production this is the client's embeddable URL (must allow framing).
                PreviewUrl = "https://tailwindcss.com",
                HeroShowcaseUrl = "https://tailwindcss.com",
                Metrics =
                [
                    new() { Label = "Online sales", Value = "+38%", Style = MetricStyle.Up, DisplayOrder = 0 },
                    new() { Label = "Load time", Value = "0.9s", Style = MetricStyle.Speed, DisplayOrder = 1 },
                    new() { Label = "Avg rating", Value = "4.9★", Style = MetricStyle.Neutral, DisplayOrder = 2 },
                ],
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Wholesale Order Pipeline",
                Description = "Automation that turns wholesale email orders into Xero invoices with no manual data entry.",
                Type = ProjectType.Automation,
                Status = ProjectStatus.Completed,
                Progress = 100,
                ClientId = harbourline.Id,
                CreatedByAdminId = admin.Id,
                IsHeroProject = true,
                HeroDisplayOrder = 1,
                HeroTagline = "Email orders to invoices, zero manual entry",
                Metrics =
                [
                    new() { Label = "Time saved", Value = "6 hrs/wk", Style = MetricStyle.Warm, DisplayOrder = 0 },
                    new() { Label = "Order accuracy", Value = "100%", Style = MetricStyle.Up, DisplayOrder = 1 },
                    new() { Label = "Per order", Value = "<2 min", Style = MetricStyle.Speed, DisplayOrder = 2 },
                ],
                PipelineSteps =
                [
                    new() { Label = "Email inbox", StepType = PipelineStepType.Trigger, DisplayOrder = 0 },
                    new() { Label = "Parse order", StepType = PipelineStepType.Process, DisplayOrder = 1 },
                    new() { Label = "Sync stock", StepType = PipelineStepType.Process, DisplayOrder = 2 },
                    new() { Label = "Xero invoice", StepType = PipelineStepType.Output, DisplayOrder = 3 },
                ],
                Integrations =
                [
                    new() { Name = "Gmail", DisplayOrder = 0 },
                    new() { Name = "Xero", DisplayOrder = 1 },
                    new() { Name = "Shopify", DisplayOrder = 2 },
                ],
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Pennine Booking Portal",
                Description = "Self-service equipment hire portal with live availability and online booking.",
                Type = ProjectType.Website,
                Status = ProjectStatus.Completed,
                Progress = 100,
                ClientId = pennine.Id,
                CreatedByAdminId = admin.Id,
                IsHeroProject = true,
                HeroDisplayOrder = 2,
                HeroTagline = "Self-service equipment booking & availability",
                // No PreviewUrl: this project demonstrates the screenshot fallback. Upload a
                // screenshot in the admin editor (stored in MinIO) — until then it shows the wireframe.
                HeroShowcaseUrl = "https://svelte.dev",
                Metrics =
                [
                    new() { Label = "Bookings", Value = "+52%", Style = MetricStyle.Up, DisplayOrder = 0 },
                    new() { Label = "Load time", Value = "1.1s", Style = MetricStyle.Speed, DisplayOrder = 1 },
                    new() { Label = "Self-service", Value = "24/7", Style = MetricStyle.Neutral, DisplayOrder = 2 },
                ],
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Fleet Maintenance Alerts",
                Description = "Predictive maintenance automation that schedules service jobs across a plant hire fleet.",
                Type = ProjectType.Automation,
                Status = ProjectStatus.Completed,
                Progress = 100,
                ClientId = pennine.Id,
                CreatedByAdminId = admin.Id,
                IsHeroProject = true,
                HeroDisplayOrder = 3,
                HeroTagline = "Predictive service reminders across the fleet",
                Metrics =
                [
                    new() { Label = "Less downtime", Value = "30%", Style = MetricStyle.Warm, DisplayOrder = 0 },
                    new() { Label = "Utilisation", Value = "+18%", Style = MetricStyle.Up, DisplayOrder = 1 },
                    new() { Label = "Missed services", Value = "0", Style = MetricStyle.Neutral, DisplayOrder = 2 },
                ],
                PipelineSteps =
                [
                    new() { Label = "Telematics", StepType = PipelineStepType.Trigger, DisplayOrder = 0 },
                    new() { Label = "Check thresholds", StepType = PipelineStepType.Process, DisplayOrder = 1 },
                    new() { Label = "Schedule job", StepType = PipelineStepType.Process, DisplayOrder = 2 },
                    new() { Label = "SMS + calendar", StepType = PipelineStepType.Output, DisplayOrder = 3 },
                ],
                Integrations =
                [
                    new() { Name = "Samsara", DisplayOrder = 0 },
                    new() { Name = "Google Calendar", DisplayOrder = 1 },
                    new() { Name = "Twilio", DisplayOrder = 2 },
                ],
            },
        };

        var adminName = $"{admin.FirstName} {admin.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(adminName)) adminName = admin.Email ?? "Fellside Digital";

        foreach (var project in projects)
        {
            project.CreatedAt = now;
            project.UpdatedAt = now;

            // Give each demo project a minimal timeline so the feed isn't empty on a fresh seed.
            project.TimelineEvents.Add(new ProjectTimelineEvent
            {
                Id = Guid.NewGuid(),
                Type = TimelineEventType.ProjectCreated,
                Summary = "Project created",
                Visibility = TimelineVisibility.ClientVisible,
                ActorId = admin.Id,
                ActorName = adminName,
                OccurredAt = now.AddDays(-30)
            });

            if (project.Status == ProjectStatus.Completed)
            {
                project.TimelineEvents.Add(new ProjectTimelineEvent
                {
                    Id = Guid.NewGuid(),
                    Type = TimelineEventType.ProjectCompleted,
                    Summary = "Project completed",
                    Visibility = TimelineVisibility.ClientVisible,
                    ActorId = admin.Id,
                    ActorName = adminName,
                    OccurredAt = now.AddDays(-1)
                });
            }
        }

        db.ClientProjects.AddRange(projects);
        await db.SaveChangesAsync();

        logger.LogInformation("DemoDataSeeder seeded {ClientCount} clients and {ProjectCount} projects.", 2, projects.Count);
    }

    private static async Task<ApplicationUser?> ResolveAdminAsync(FellsideDigitalDbContext db, UserManager<ApplicationUser> userManager)
    {
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var byEmail = await userManager.FindByEmailAsync(adminEmail);
            if (byEmail is not null) return byEmail;
        }

        var admins = await userManager.GetUsersInRoleAsync("SiteAdmin");
        return admins.FirstOrDefault();
    }

    private static async Task<ApplicationUser?> EnsureClientAsync(
        UserManager<ApplicationUser> userManager, ILogger logger,
        string email, string firstName, string lastName, string company)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            CompanyName = company,
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (result.Succeeded) return user;

        logger.LogWarning("DemoDataSeeder failed to create client {Email}: {Errors}",
            email, string.Join("; ", result.Errors.Select(e => e.Description)));
        return null;
    }
}
