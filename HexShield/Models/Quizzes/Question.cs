using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
namespace HexShield.Models.Quizzes;
public class Question : BaseEntity
{
    [Required]
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    [Required,MaxLength(2000)]
    public string Text { get; set; } = string.Empty;
    [Required, MaxLength(30)]
    public string QuestionType { get; set; } = "SingleChoice";
    [Range(1, 100)]
    public int Marks { get; set; } = 1;
    [Range(1, int.MaxValue)]
    public int SortOrder { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}