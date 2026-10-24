using Microsoft.AspNetCore.Mvc;
using HexShield.Infrastructure.Tenancy;
using HexShield.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using HexShield.Shared.DTOs;

namespace HexShield.Controllers;

[EnableRateLimiting("AuthPolicy")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ITenantContext tenantContext, ILogger<AuthController> logger)
    {
        _authService = authService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        if (!request.TenantId.HasValue && _tenantContext.HasTenant)
        {
            request.TenantId = _tenantContext.TenantId;
        }

        var response = await _authService.RegisterAsync(request, GetClientIpAddress());
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);
        try
        {
            var response = await _authService.LoginAsync(request, GetClientIpAddress());
            SetRefreshTokenCookie(response.RefreshToken);
            _logger.LogInformation("Login successful for email: {Email}", request.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Login failed for {Email}: {Message}", request.Email, ex.Message);
            throw;
        }
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto? request)
    {
        var refreshToken = !string.IsNullOrWhiteSpace(request?.RefreshToken)
            ? request.RefreshToken.Trim()
            : Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        // Allow extracting access token from body OR Authorization header (optional if refresh token hash is valid)
        var accessToken = request?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken) && Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerStr = authHeader.ToString();
            if (headerStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                accessToken = headerStr["Bearer ".Length..].Trim();
            }
        }

        var dto = new RefreshTokenRequestDto(accessToken ?? string.Empty, refreshToken);
        var response = await _authService.RefreshTokenAsync(dto, GetClientIpAddress());
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("revoke-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto? request)
    {
        var refreshToken = !string.IsNullOrWhiteSpace(request?.RefreshToken)
            ? request.RefreshToken.Trim()
            : Request.Cookies["refreshToken"];

        Response.Cookies.Delete("refreshToken");

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Ok(new { message = "Session cleared." });
        }

        await _authService.RevokeTokenAsync(refreshToken, GetClientIpAddress());
        return Ok(new { message = "Token revoked successfully." });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var isHttps = Request?.IsHttps ?? false;
        var cookieOption = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/"
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOption);
    }
}
