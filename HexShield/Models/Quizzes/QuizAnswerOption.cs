using HexShield.Models.Common;

namespace HexShield.Models.Quizzes;
public class QuizAnswerOption : BaseEntity
{
    public int QuizAnswerId { get; set; }
    public QuizAnswer QuizAnswer { get; set; } = null!;
    public int QuestionOptionId { get; set; }
    public QuestionOption QuestionOption { get; set; } = null!;
}
