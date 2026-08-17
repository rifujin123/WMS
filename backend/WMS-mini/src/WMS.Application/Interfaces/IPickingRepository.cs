using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IPickingRepository
{
    Task<List<Picking>> GetAllAsync();
    Task<Picking?> GetByIdAsync(Guid id);
    Task AddAsync(Picking picking);
    Task UpdateAsync(Picking picking);
    Task DeleteAsync(Picking picking);
    Task<List<Guid>> GetPickingIdsExceptAsync(Guid excludeId);
    Task<List<Picking>> GetOpenBySaleOrderIdAsync(Guid saleOrderId);
}
