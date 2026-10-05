using System.ComponentModel.DataAnnotations;

namespace HexShield.Models.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string UserId,
    string Email,
    IEnumerable<string> Roles,
    int TenantId
);
public record LoginRequestDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    public LoginRequestDto() { }

    public LoginRequestDto(string email, string password)
    {
        Email = email;
        Password = password;
    }
}

public record RegisterRequestDto
{
    [Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
    public string Password { get; set; } = string.Empty;

    public int? TenantId { get; set; }
    public string? Department { get; set; }

    public RegisterRequestDto() { }

    public RegisterRequestDto(string firstName, string lastName, string email, string password, int? tenantId = null, string? department = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;
        TenantId = tenantId;
        Department = department;
    }
}

public record RefreshTokenRequestDto(
    string AccessToken,
    string RefreshToken
);

public record RevokeTokenRequestDto(
    string? RefreshToken = null
);
