using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
namespace HexShield.Models.Quizzes;
public class QuestionOption : BaseEntity
{
    [Required]
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    [Required,MaxLength(1000)]
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    [Range(1, int.MaxValue)]
    public int SortOrder { get; set; }
}