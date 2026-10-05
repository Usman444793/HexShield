using System.ComponentModel.DataAnnotations;
using HexShield.Models.Academic;
using HexShield.Models.Common;
namespace HexShield.Models.Quizzes;
public class Quiz : BaseEntity,IMultiTenant
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    [Required]
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    [Required,MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string? Instructions { get; set; }
    [Range(1, 1000)]
    public int TotalMarks { get; set; }
    [Range(1,600)]
    public int DurationMinutes { get; set; } = 60;
    public int MaxAttempts { get; set; } = 1;
    public decimal PassPercentage { get; set; } = 70.0m;
    public bool IsPublished { get; set; } = false;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}