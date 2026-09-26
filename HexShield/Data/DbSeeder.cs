using HexShield.Models.Common;
using HexShield.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace HexShield.Data;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // Make sure a tenant exists
        var tenant = await dbContext.Tenants
            .FirstOrDefaultAsync();

        if (tenant == null)
        {
            tenant = new Tenant
            {
                Name = "HexShield",
                Identifier = "hexshield",
                IsActive = true
            };

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync();
        }
        var adminRole = await roleManager.FindByNameAsync("Admin");

        if (adminRole == null)
        {
            adminRole = new ApplicationRole
            {
                Name = LmsRoles.Admin,
                NormalizedName = LmsRoles.Admin.ToUpperInvariant(),
                Description = "System administrator",
                TenantId = tenant.Id
            };

            await roleManager.CreateAsync(adminRole);
        }
        const string adminEmail = "admin@hexshield.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "HexShield Administrator",
                TenantId = tenant.Id,
                IsActive = true,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(
                admin,
                "Admin@12345678"
            );
            if (!result.Succeeded)
            {
                throw new Exception(
                    "Failed to create admin: " +
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
            }
        }
        if (!await userManager.IsInRoleAsync(admin, LmsRoles.Admin))
        {
            await userManager.AddToRoleAsync(admin, LmsRoles.Admin);
        }
    }
}