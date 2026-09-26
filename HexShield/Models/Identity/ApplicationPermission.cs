using System.ComponentModel.DataAnnotations;

namespace HexShield.Models.Identity;

public class ApplicationPermission
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty; // e.g., "Users.Create", "Course.Edit"

    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    public bool IsSystemPermission { get; set; } = false;
}
