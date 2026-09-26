using HexShield.Models.Common;
using HexShield.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HexShield.Data;

public static class DbInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds essential bootstrap data (default tenant + LMS roles).
    /// Safe to call on every startup — all operations are idempotent.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // Apply any pending EF Core migrations automatically
            await db.Database.MigrateAsync();

            // ── 1. Seed default Tenant ────────────────────────────────────────────
            // Bypass global query filter so we can always read the tenant table
            var defaultTenantExists = await db.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Identifier == "default");

            if (!defaultTenantExists)
            {
                db.Tenants.Add(new Tenant
                {
                    Name = "Default Organization",
                    Identifier = "default",
                    IsActive = true
                    // CreatedAt / IsDeleted set automatically by ProcessAuditingAndSoftDelete
                });
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded default tenant.");
            }
            // ── 2. Seed LMS roles ─────────────────────────────────────────────────
            foreach (var roleName in LmsRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = roleName,
                        Description = $"System role: {roleName}"
                    });

                    if (result.Succeeded)
                        logger.LogInformation("Seeded role: {Role}", roleName);
                    else
                        logger.LogWarning("Failed to seed role {Role}: {Errors}", roleName,
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database initialization failed. Application cannot start safely.");
            throw;
        }
    }
}
