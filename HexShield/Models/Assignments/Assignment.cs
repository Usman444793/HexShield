using System.ComponentModel.DataAnnotations;
using HexShield.Models.Academic;
using HexShield.Models.Common;
namespace HexShield.Models.Assignments;
public class Assignment : BaseEntity
{
    [Required]
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    [Required,MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required,MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    [Required]
    public DateTimeOffset DueDate {  get; set; }
    [Range(1, 1000)]
    public int MaxScore { get; set; } = 100;
    public bool IsPublished { get; set; } = false;
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
