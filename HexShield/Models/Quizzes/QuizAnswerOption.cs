using HexShield.Models.Common;

namespace HexShield.Models.Quizzes;
public class QuizAnswerOption
{
    public int QuizAnswerId { get; set; }
    public QuizAnswer? QuizAnswer { get; set; }
    public int QuestionOptionId { get; set; }
    public QuestionOption? QuestionOption { get; set; }
}
