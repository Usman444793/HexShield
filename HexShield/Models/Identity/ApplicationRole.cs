using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace HexShield.Models.Identity;
public class ApplicationRole : IdentityRole
{
    public int? TenantId { get; set; }
    [MaxLength(250)] 
    public string? Description { get; set; } 
    public ICollection<UserOrganizationRole> OrganizationRoles { get; set; } = new List<UserOrganizationRole>();
}
