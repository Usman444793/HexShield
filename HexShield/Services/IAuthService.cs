using HexShield.Shared.DTOs;

namespace HexShield.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, string ipAddress);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, string ipAddress);
    Task RevokeTokenAsync(string refreshToken, string ipAddress);
}