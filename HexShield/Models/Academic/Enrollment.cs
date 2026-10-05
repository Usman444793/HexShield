using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
using HexShield.Models.Identity;
namespace HexShield.Models.Academic;
public enum EnrollmentStatus
{
    Active = 1,
    Completed = 2,
    Dropped = 3,
    Suspended = 4
}
public class Enrollment : BaseEntity, IMultiTenant
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    [Required]
    public string StudentProfileId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}