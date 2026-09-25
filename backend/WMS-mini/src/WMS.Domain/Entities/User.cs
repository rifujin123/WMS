using Microsoft.AspNetCore.Identity;

namespace WMS.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}
