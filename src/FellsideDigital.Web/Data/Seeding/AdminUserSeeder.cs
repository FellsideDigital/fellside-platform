using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Data.Seeding;

public static class AdminUserSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FellsideDigitalDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Pull credentials from environment variables
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL")
                         ?? throw new InvalidOperationException("ADMIN_EMAIL not set");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
                            ?? throw new InvalidOperationException("ADMIN_PASSWORD not set");

        // Create roles if they don't exist
        string[] roles = new[] { "SiteAdmin", "AuctionAdmin" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Failed to create role '{roleName}': {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        // Seed admin user
        var admin = await db.Users.SingleOrDefaultAsync(u => u.Email == adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                NormalizedUserName = adminEmail.ToUpper(),
                Email = adminEmail,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Assign admin user to SiteAdmin role
        if (!await userManager.IsInRoleAsync(admin, "SiteAdmin"))
        {
            var addRoleResult = await userManager.AddToRoleAsync(admin, "SiteAdmin");
            if (!addRoleResult.Succeeded)
            {
                throw new Exception($"Failed to assign admin to SiteAdmin role: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        // Assign admin user to AuctionAdmin role
        if (!await userManager.IsInRoleAsync(admin, "AuctionAdmin"))
        {
            var addRoleResult = await userManager.AddToRoleAsync(admin, "AuctionAdmin");
            if (!addRoleResult.Succeeded)
            {
                throw new Exception($"Failed to assign admin to AuctionAdmin role: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}
