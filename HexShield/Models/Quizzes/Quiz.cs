using System.ComponentModel.DataAnnotations;
using HexShield.Models.Academic;
using HexShield.Models.Common;
namespace HexShield.Models.Quizzes;
public class Quiz : BaseEntity
{
    [Required]
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    [Required,MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [Range(1, 1000)]
    public int TotalMarks { get; set; }
    [Range(1,600)]
    public int DurationMinutes { get; set; } = 60;
    public bool IsPublished { get; set; } = false;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}