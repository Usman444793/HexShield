using Microsoft.AspNetCore.Identity;
using HexShield.Models.Academic;
using HexShield.Models.Common;
using System.ComponentModel.DataAnnotations;
namespace HexShield.Models.Identity;
public class ApplicationUser : IdentityUser,IMultiTenant
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    [Required,MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
    public TeacherProfile? TeacherProfile { get; set; }
    public StudentProfile? StudentProfile { get; set; }
    public ICollection<UserOrganizationRole> OrganizationRoles { get; set; } = new List<UserOrganizationRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}