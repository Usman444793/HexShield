using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
using HexShield.Models.Identity;
using HexShield.Shared.Enums;

namespace HexShield.Models.Academic;

public class Submission : BaseEntity, IMultiTenant
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    [Required]
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;
    public string? Content { get; set; }
    [MaxLength(2000)]
    public string? AttachmentUrl { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public decimal Grade { get; set; }
    public string? Feedback { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
}