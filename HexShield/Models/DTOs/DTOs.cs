namespace HexShield.Models.DTOs;
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiration,
    string UserId,
    string Email,
    IEnumerable<string> Roles,
    int TenantId = 0
);
public record LoginRequestDto(
    string Email,
    string Password
);
public record RegisterRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    int? TenantId = null,
    string? Department = null
);
public record RefreshTokenRequestDto(
    string AccessToken,
    string RefreshToken
);
public record RevokeTokenRequestDto(
    string? RefreshToken = null
);