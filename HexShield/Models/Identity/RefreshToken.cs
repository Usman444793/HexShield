using System.ComponentModel.DataAnnotations;
namespace HexShield.Models.Identity;
public class RefreshToken
{
    public int Id { get; set; }
    [Required]
    public string UserId { get; set; } = default!;
    public ApplicationUser? User { get; set; }
    [Required]
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedByIp { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public bool IsActive => !IsRevoked && !IsExpired;
}
