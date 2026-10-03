using Microsoft.AspNetCore.Mvc;
using HexShield.Infrastructure.Tenancy;
using HexShield.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using HexShield.Models.DTOs;

namespace HexShield.Controllers;

[EnableRateLimiting("AuthPolicy")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITenantContext _tenantContext;

    public AuthController(IAuthService authService, ITenantContext tenantContext)
    {
        _authService = authService;
        _tenantContext = tenantContext;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        // If TenantId was not explicitly passed in body, inherit from resolved tenant middleware
        var effectiveRequest = request.TenantId.HasValue || !_tenantContext.HasTenant
            ? request
            : request with { TenantId = _tenantContext.TenantId };

        var response = await _authService.RegisterAsync(effectiveRequest, GetClientIpAddress());
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        Console.WriteLine($"[AUTH] Login request received for: {request.Email}");
        try 
        {
            var response = await _authService.LoginAsync(request, GetClientIpAddress());
            SetRefreshTokenCookie(response.RefreshToken);
            Console.WriteLine($"[AUTH] Login successful for: {request.Email}");
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH] Login failed for {request.Email}: {ex.Message}");
            throw;
        }
    }

    [HttpGet("debug-user")]
    [AllowAnonymous]
    public async Task<IActionResult> DebugUser(string email)
    {
        Console.WriteLine($"[DEBUG] Checking existence of user: {email}");
        // This is a temporary endpoint to verify DB seeding
        var user = await _authService.LoginAsync(new LoginRequestDto(email, "AdminPassword123!"), "debug-ip");
        return Ok(new { exists = true, userId = user.UserId });
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto? request)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        // Allow extracting access token from body OR Authorization header
        var accessToken = request?.AccessToken;
        if (string.IsNullOrEmpty(accessToken) && Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerStr = authHeader.ToString();
            if (headerStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                accessToken = headerStr["Bearer ".Length..].Trim();
            }
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { message = "Access token is required to refresh session." });
        }

        var dto = new RefreshTokenRequestDto(accessToken, refreshToken);
        var response = await _authService.RefreshTokenAsync(dto, GetClientIpAddress());
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("revoke-token")]
    [AllowAnonymous] // Allow revoking session even if access token is already expired
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto? request)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        await _authService.RevokeTokenAsync(refreshToken, GetClientIpAddress());
        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Token revoked successfully." });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOption = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOption);
    }
}
