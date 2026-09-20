using HexShield.Models.Academic;
using HexShield.Models.Identity;
using System.ComponentModel.DataAnnotations;

namespace HexShield.Models.Common;
public class OrganizationNode: BaseEntity,IMultiTenant
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    [Required,MaxLength(20)]
    public string Code { get; set; } = string.Empty;
    [Required, MaxLength(30)]
    public string NodeType { get; set; } = "Department";
    public int? ParentId { get; set; }
    public OrganizationNode? Parent { get; set; }
    public ICollection<OrganizationNode> Children { get; set; } = new List<OrganizationNode>();
    [MaxLength(500)]
    public string HierarchyPath { get; set; } = string.Empty;
    public ICollection<UserOrganizationRole> UserOrganizationRoles { get; set; } = new List<UserOrganizationRole>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
