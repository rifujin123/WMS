using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class LocationDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Aisle { get; set; } = string.Empty;
    public string Rack { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public LocationType LocationType { get; set; }
    public int MaxQuantity { get; set; }
    public int CurrentQuantity { get; set; }
}
