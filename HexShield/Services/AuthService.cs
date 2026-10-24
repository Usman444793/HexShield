using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HexShield.Data;
using HexShield.Models.Academic;
using HexShield.Models.Common;
using HexShield.Shared.DTOs;
using HexShield.Shared.Enums;
using HexShield.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HexShield.Infrastructure.Tenancy;

namespace HexShield.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantContext _tenantContext;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtService _jwtService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtService jwtService,
        ApplicationDbContext dbContext,
        ILogger<AuthService> logger,
        ITenantContext tenantContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _dbContext = dbContext;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");

        int tenantId;
        if (request.TenantId.HasValue && request.TenantId.Value > 0)
        {
            var tenantExists = await _dbContext.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Id == request.TenantId.Value && !t.IsDeleted && t.IsActive);
            if (!tenantExists)
                throw new InvalidOperationException($"Tenant with ID {request.TenantId.Value} does not exist or is inactive.");
            tenantId = request.TenantId.Value;
        }
        else
        {
            var defaultTenant = await _dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => !t.IsDeleted && t.IsActive);
            if (defaultTenant == null)
            {
                defaultTenant = new Tenant
                {
                    Name = "Default Organization",
                    Identifier = "default",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.Tenants.Add(defaultTenant);
                await _dbContext.SaveChangesAsync();
            }
            tenantId = defaultTenant.Id;
        }

        _tenantContext.SetTenant(tenantId);

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existingUser = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.NormalizedEmail == normalizedEmail);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists in this organization.");

        var fullName = string.IsNullOrWhiteSpace(request.LastName)
            ? request.FirstName.Trim()
            : $"{request.FirstName.Trim()} {request.LastName.Trim()}";

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FullName = fullName,
            TenantId = tenantId,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        if (!await _roleManager.RoleExistsAsync(LmsRoles.Student))
        {
            await _roleManager.CreateAsync(new ApplicationRole
            {
                Name = LmsRoles.Student,
                NormalizedName = LmsRoles.Student.ToUpperInvariant(),
                Description = "Default student role",
                TenantId = tenantId
            });
        }
        await _userManager.AddToRoleAsync(user, LmsRoles.Student);

        var studentNumber = $"STU-{DateTimeOffset.UtcNow.Year}-{Random.Shared.Next(10000, 99999)}";
        var studentProfile = new StudentProfile
        {
            UserId = user.Id,
            StudentNumber = studentNumber,
            Department = string.IsNullOrWhiteSpace(request.Department) ? "General" : request.Department.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = user.Id
        };
        _dbContext.StudentProfiles.Add(studentProfile);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("New student registered: {Email} (UserId: {UserId}, StudentNumber: {StudentNumber}, TenantId: {TenantId})",
            user.Email, user.Id, studentNumber, tenantId);

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        _tenantContext.SetTenant(user.TenantId);

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for deactivated user {UserId} from IP {IpAddress}", user.Id, ipAddress);
            throw new UnauthorizedAccessException("Account has been deactivated. Please contact an administrator.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login attempt for locked out user {UserId} from IP {IpAddress}", user.Id, ipAddress);
            throw new UnauthorizedAccessException("Account is temporarily locked out due to multiple failed login attempts. Please try again later.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("User {UserId} reached max failed attempts and is now locked out from IP {IpAddress}", user.Id, ipAddress);
                throw new UnauthorizedAccessException("Account has been locked out due to multiple failed attempts. Please try again later.");
            }
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {UserId} logged in successfully from IP {IpAddress}", user.Id, ipAddress);

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedAccessException("Refresh token is required.");

        string? tokenUserId = null;
        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            try
            {
                var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
                tokenUserId = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                           ?? principal?.FindFirstValue("nameid");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Access token validation failed during refresh; falling back to refresh token hash lookup.");
            }
        }

        var hashedRefreshToken = _jwtService.HashToken(request.RefreshToken.Trim());
        var existingRefreshToken = await _dbContext.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == hashedRefreshToken);

        if (existingRefreshToken == null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!string.IsNullOrEmpty(tokenUserId) && !string.Equals(existingRefreshToken.UserId, tokenUserId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Refresh token does not match the authenticated user.");

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == existingRefreshToken.UserId);

        if (user == null)
            throw new UnauthorizedAccessException("User not found.");

        _tenantContext.SetTenant(user.TenantId);

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account has been deactivated.");

        if (existingRefreshToken.IsRevoked)
        {
            _logger.LogWarning("SECURITY ALERT: Revoked refresh token reuse detected for User {UserId} from IP {IpAddress}. Revoking all active sessions.", user.Id, ipAddress);
            var activeTokens = await _dbContext.RefreshTokens
                .AsTracking()
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTimeOffset.UtcNow;
                token.RevokedByIp = ipAddress;
                token.ReplacedByTokenHash = "REVOKED_DUE_TO_REPLAY_ATTACK";
            }
            await _dbContext.SaveChangesAsync();
            throw new UnauthorizedAccessException("Security violation: Token reuse detected. All active sessions have been terminated. Please log in again.");
        }

        if (existingRefreshToken.IsExpired)
            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");

        // Rotate refresh token (one-time use)
        existingRefreshToken.IsRevoked = true;
        existingRefreshToken.RevokedAt = DateTimeOffset.UtcNow;
        existingRefreshToken.RevokedByIp = ipAddress;

        var (newAccessToken, expiration) = await _jwtService.GenerateAccessTokenAsync(user);
        var newRawRefreshToken = _jwtService.GenerateRefreshToken();
        var newHashedRefreshToken = _jwtService.HashToken(newRawRefreshToken);

        existingRefreshToken.ReplacedByTokenHash = newHashedRefreshToken;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newHashedRefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        _logger.LogInformation("Refresh token rotated successfully for User {UserId} from IP {IpAddress}", user.Id, ipAddress);
        return new AuthResponseDto(newAccessToken, newRawRefreshToken, expiration, user.Id, user.Email!, roles, user.TenantId);
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hashedToken = _jwtService.HashToken(refreshToken.Trim());
        var token = await _dbContext.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken);

        if (token == null || !token.IsActive)
            return;

        token.IsRevoked = true;
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.RevokedByIp = ipAddress;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Refresh token revoked successfully from IP {IpAddress}", ipAddress);
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user, string ipAddress)
    {
        var (accessToken, expiration) = await _jwtService.GenerateAccessTokenAsync(user);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var hashedRefreshToken = _jwtService.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        return new AuthResponseDto(accessToken, rawRefreshToken, expiration, user.Id, user.Email!, roles, user.TenantId);
    }
}
