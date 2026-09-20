using HexShield.Models.Common;
using HexShield.Models.Quizzes;
using System.ComponentModel.DataAnnotations;
namespace HexShield.Models.Quizzes;
public class QuizAnswer : BaseEntity
{
    public int QuizAttemptId { get; set; }
    public QuizAttempt QuizAttempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public string? TextResponse { get; set; }
    public bool IsGraded { get; set; }
    [Range(0,1000)]
    public int AwardedMarks { get; set; }
    public string? TeacherFeedback { get; set; }
    public ICollection<QuizAnswerOption> SelectedOptions { get; set; } = new List<QuizAnswerOption>();
}