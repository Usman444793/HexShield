namespace HexShield.Infrastructure.Tenancy;

public interface ITenantContext
{
    int TenantId { get; set; }
    string? TenantIdentifier { get; set; }
    bool HasTenant => TenantId > 0;
    void SetTenant(int tenantId, string? identifier = null);
}

public class TenantContext : ITenantContext
{
    public int TenantId { get; set; }
    public string? TenantIdentifier { get; set; }

    public void SetTenant(int tenantId, string? identifier = null)
    {
        TenantId = tenantId;
        TenantIdentifier = identifier;
    }
}