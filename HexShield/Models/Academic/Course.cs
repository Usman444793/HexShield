using System.ComponentModel.DataAnnotations;
using HexShield.Models.Assignments;
using HexShield.Models.Common;
using HexShield.Models.Quizzes;

namespace HexShield.Models.Academic;
public class Course : BaseEntity,IMultiTenant
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int OrganizationNodeId { get; set; }
    public OrganizationNode OrganizationNode { get; set; } = null!;
    [Required,MaxLength(30)]
    public string CourseCode { get; set; } = string.Empty;
    [Required,MaxLength(50)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string? Description { get; set; }
    [Required]
    public int PrimaryTeacherProfileId { get; set; }
    public TeacherProfile PrimaryTeacherProfile { get; set; } = null!;
    public bool IsPublished { get; set; } = false;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}