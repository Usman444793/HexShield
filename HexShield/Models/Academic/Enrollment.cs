using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
namespace HexShield.Models.Academic;
public class Enrollment : BaseEntity
{
    [Required]
    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;
    [Required]
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public DateTimeOffset EnrolledAt { get; set; } = DateTimeOffset.UtcNow;
    [Required,MaxLength(30)]
    public string Status { get; set; } = "Active";
}
