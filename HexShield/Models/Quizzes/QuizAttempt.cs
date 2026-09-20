using System.ComponentModel.DataAnnotations;
using HexShield.Models.Academic;
using HexShield.Models.Common;
namespace HexShield.Models.Quizzes;
public class QuizAttempt : BaseEntity
{
    [Required]
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    [Required]
    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    [Range(0, 1000)]
    public int? Score { get; set; }
    public bool IsCompleted { get; set; } = false;
    public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
}