namespace WMS.Application.DTOs;

public class LocationListQuery
{
    public int Page { get; init; } = 1;
    public Guid? WarehouseId { get; init; }
}
