using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IShipmentRepository
{
    Task<List<Shipment>> GetAllAsync();
    Task<Shipment?> GetByIdAsync(Guid id);
    Task AddAsync(Shipment shipment);
    Task UpdateAsync(Shipment shipment);
    Task DeleteAsync(Shipment shipment);
}
