using System.ComponentModel.DataAnnotations;
using HexShield.Models.Academic;
using HexShield.Models.Common;
namespace HexShield.Models.Quizzes;
public class QuizAttempt : BaseEntity,IMultiTenant
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    [Required]
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    [Required]
    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;
    public string StudentId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    [Range(0, 1000)]
    public decimal Score { get; set; }
    public decimal TotalPossibleScore { get; set; } = 0m;
    public bool IsCompleted { get; set; } = false;
    public bool Passed { get; set; } = false;
    public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
}