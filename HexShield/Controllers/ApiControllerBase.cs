using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace HexShield.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected string GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.ToString().Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(ip)) return ip;
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
    }

    protected int GetCurrentTenantId()
    {
        var tenantClaim = User.FindFirst("tenantId")?.Value;
        return int.TryParse(tenantClaim, out var tenantId) ? tenantId : 0;
    }

    protected string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}