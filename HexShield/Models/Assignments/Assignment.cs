using HexShield.Models.Common;
using System.ComponentModel.DataAnnotations;
namespace HexShield.Models.Academic;
public class Assignment : BaseEntity, IMultiTenant
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public decimal MaxScore { get; set; } = 100;
    public bool IsPublished { get; set; } = false;
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}