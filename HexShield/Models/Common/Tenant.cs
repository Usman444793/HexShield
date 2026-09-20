using System.ComponentModel.DataAnnotations;
using HexShield.Models.Academic;
using HexShield.Models.Identity;
namespace HexShield.Models.Common;
public class Tenant : BaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50)]
    public string Identifier { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? CustomDomain { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<OrganizationNode> OrganizationNodes { get; set; } = new List<OrganizationNode>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
