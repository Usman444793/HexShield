using HexShield.Models.Academic;
using HexShield.Models.Common;
using HexShield.Models.Identity;
using System.ComponentModel.DataAnnotations;
namespace HexShield.Models.Assignments;
public class Submission : BaseEntity
{
    [Required]
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    [Required]
    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;
    [Required,MaxLength(10000)]
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    [Range(0,1000)]
    public int? Score { get; set; }
    [MaxLength(4000)]
    public string? Feedback { get; set; }
    public DateTimeOffset? GradedAt { get; set; }
    public string? GradedById { get; set; }
    public ApplicationUser? GradedBy { get; set; }
}
