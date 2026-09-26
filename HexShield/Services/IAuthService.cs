using HexShield.Models.DTOs;
namespace HexShield.Services;
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, string IpAddress);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string IpAddress);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, string IpAddress);
    Task RevokeTokenAsync(string RefreshToken, string IpAddress);
}