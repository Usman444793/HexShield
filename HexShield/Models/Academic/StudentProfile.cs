using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
using HexShield.Models.Identity;
using HexShield.Models.Assignments;
using HexShield.Models.Progress;
using HexShield.Models.Quizzes;
namespace HexShield.Models.Academic;
public class StudentProfile: BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    [Required,MaxLength(50)]
    public string StudentNumber { get; set; } = string.Empty;
    [Required,MaxLength(100)]
    public string Department { get; set; } = string.Empty;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
}