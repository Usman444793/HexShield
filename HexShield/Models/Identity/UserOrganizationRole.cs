using HexShield.Models.Common;

namespace HexShield.Models.Identity;
public class UserOrganizationRole : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string RoleId { get; set; } = string.Empty;
    public ApplicationRole Role { get; set; } = null!;
    public int OrganizationNodeId { get; set; }
    public OrganizationNode OrganizationNode { get; set; } = null!;
}
