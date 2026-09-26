using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HexShield.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using HexShield.Data;
using Microsoft.EntityFrameworkCore;
namespace HexShield.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public JwtService(IConfiguration configuration, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
    {
        _configuration = configuration;
        _userManager = userManager;
        _dbContext = dbContext;
    }
    public async Task<(string Token, DateTimeOffset Expiration)> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is missing");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiryMinutesConfig = _configuration["Jwt:ExpiryMinutes"];
        var expiryMinutes = double.TryParse(expiryMinutesConfig, out var parsedMinutes) ? parsedMinutes : 60;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("fullName", user.FullName),
            new Claim("tenantId", user.TenantId.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var orgRoles = await _dbContext.UserOrganizationRoles.Where(uor => uor.UserId == user.Id && !uor.IsDeleted)
            .Include(uor => uor.Role).ToListAsync();
        foreach (var orgRole in orgRoles)
        {
            if (orgRole.Role != null && !string.IsNullOrEmpty(orgRole.Role.Name))
            {
                claims.Add(new Claim("org_role", $"{orgRole.OrganizationNodeId}:{orgRole.Role.Name}"));
                // Add the orgNodeId as a separate claim for the PermissionHandler to use
                claims.Add(new Claim("orgNodeId", orgRole.OrganizationNodeId.ToString()));
            }
        }

        var expiration = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration.UtcDateTime,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiration);
    }
    public string GenerateRefreshToken()
    {
        var randomNumber = new Byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secretkey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is missing");
        var TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretkey)),

        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, TokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token algorithm");
        }
        return principal;
    }
}
