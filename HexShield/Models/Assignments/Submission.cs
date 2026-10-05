using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
using HexShield.Models.Identity;
namespace HexShield.Models.Academic;
public enum SubmissionStatus
{
    Submitted = 1,
    Graded = 2,
    Returned = 3
}
public class Submission : BaseEntity, IMultiTenant
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    [Required]
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;
    [MaxLength(4000)]
    public string? Content { get; set; }
    [MaxLength(500)]
    public string? AttachmentUrl { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public decimal Grade { get; set; }
    [MaxLength(1000)]
    public string? Feedback { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
}