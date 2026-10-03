using HexShield.Models.Common;
using HexShield.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HexShield.Data;

public static class DbInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds essential bootstrap data (default tenant, LMS roles, default admin).
    /// Safe to call on every startup — all operations are idempotent.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // ── 1. Apply EF Core Migrations ─────────────────────────────────────────
            await db.Database.MigrateAsync();

            // ── 2. Seed Default Tenant ─────────────────────────────────────────────
            var defaultTenant = await db.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Identifier == "default" || t.Id == 1);

            if (defaultTenant == null)
            {
                defaultTenant = new Tenant
                {
                    Name = "Default Organization",
                    Identifier = "default",
                    IsActive = true
                };
                db.Tenants.Add(defaultTenant);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded default tenant with ID {TenantId}.", defaultTenant.Id);
            }

            // ── 3. Seed LMS Roles ──────────────────────────────────────────────────
            foreach (var roleName in LmsRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant(),
                        Description = $"System role: {roleName}",
                        TenantId = defaultTenant.Id
                    });

                    if (result.Succeeded)
                        logger.LogInformation("Seeded role: {Role}", roleName);
                    else
                        logger.LogWarning("Failed to seed role {Role}: {Errors}", roleName,
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // ── 4. Seed Default Admin User ─────────────────────────────────────────
            const string adminEmail = "admin@hexshield.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "HexShield Administrator",
                    TenantId = defaultTenant.Id,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin@12345678!");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, LmsRoles.Admin);
                    logger.LogInformation("Seeded default admin user: {Email}", adminEmail);
                }
                else
                {
                    logger.LogWarning("Failed to seed admin user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
            else if (!await userManager.IsInRoleAsync(adminUser, LmsRoles.Admin))
            {
                await userManager.AddToRoleAsync(adminUser, LmsRoles.Admin);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database initialization failed. Application cannot start safely.");
            throw;
        }
    }
}
