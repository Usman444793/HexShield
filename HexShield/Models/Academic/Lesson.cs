using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
using HexShield.Models.Progress;
namespace HexShield.Models.Academic;
public class Lesson : BaseEntity
{
    [Required]
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    [Required,MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required,MaxLength(10000)]
    public string Content { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? VideoUrl { get; set; }
    [Range(1,int.MaxValue)]
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();
}
