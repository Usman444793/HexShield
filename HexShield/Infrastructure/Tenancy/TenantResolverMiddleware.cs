using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace HexShield.Infrastructure.Tenancy;

public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // 1. Authenticated User Claim takes top priority for security
        var userTenantClaim = context.User.FindFirst("tenantId")?.Value;
        if (int.TryParse(userTenantClaim, out var claimTenantId) && claimTenantId > 0)
        {
            // Security check: if client also sent X-Tenant-Id header, ensure it matches user's tenant
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var reqTenantHeader) &&
                int.TryParse(reqTenantHeader.ToString(), out var headerTenantId) &&
                headerTenantId != claimTenantId)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "Cross-tenant access forbidden." });
                return;
            }

            tenantContext.SetTenant(claimTenantId);
            await _next(context);
            return;
        }

        // 2. Explicit Header for anonymous / pre-auth requests
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) &&
            int.TryParse(tenantHeader.ToString(), out var parsedTenantId) &&
            parsedTenantId > 0)
        {
            tenantContext.SetTenant(parsedTenantId);
        }
        else if (context.Request.Headers.TryGetValue("X-Tenant-Identifier", out var identifierHeader))
        {
            tenantContext.SetTenant(0, identifierHeader.ToString().Trim());
        }
        else
        {
            // 3. Subdomain-based tenant resolution (e.g., tenant1.hexshield.com)
            var host = context.Request.Host.Host;
            if (!string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                var parts = host.Split('.');
                if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    var subdomain = parts[0].Trim().ToLowerInvariant();
                    if (subdomain != "www" && subdomain != "api")
                    {
                        tenantContext.SetTenant(0, subdomain);
                    }
                }
            }
        }

        await _next(context);
    }
}