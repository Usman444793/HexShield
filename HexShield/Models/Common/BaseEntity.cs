using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HexShield.Models.Common;
public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedById { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedById { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedById { get; set; }
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
public interface IMultiTenant
{
    public int TenantId { get; set; }
}