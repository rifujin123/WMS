using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IShipmentService
{
    Task<List<ShipmentDto>> GetAllAsync();
    Task<ShipmentDto?> GetByIdAsync(Guid id);
    Task<ShipmentDto?> GetBySaleOrderAsync(Guid saleOrderId);
    Task<ShipmentDto> CreateAsync(CreateShipmentDto dto, Guid userId);
}
