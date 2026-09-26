using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HexShield.Models.Identity;

public class RolePermission
{
    [Key]
    public string RoleId { get; set; } = string.Empty;
    public ApplicationRole Role { get; set; } = null!;
    
    [Key]
    public int PermissionId { get; set; }
    public ApplicationPermission Permission { get; set; } = null!;
}

