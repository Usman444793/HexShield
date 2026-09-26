using System.Security.Claims;
using HexShield.Models.Identity;
namespace HexShield.Services;
public interface IJwtService
{
    Task<(string Token, DateTimeOffset Expiration)> GenerateAccessTokenAsync(ApplicationUser user);
    string GenerateRefreshToken();
    string HashToken(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}