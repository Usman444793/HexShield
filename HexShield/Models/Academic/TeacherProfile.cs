using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
using HexShield.Models.Identity;
namespace HexShield.Models.Academic;
public class TeacherProfile : BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    [Required,MaxLength(50)]
    public string EmployeeNumber { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string? Bio { get; set; }
    public ICollection<Course> PrimaryCourses { get; set; } = new List<Course>();
}
