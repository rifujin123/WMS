using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface ISaleOrderRepository
{
    Task<List<SaleOrder>> GetAllAsync();
    Task<SaleOrder?> GetByIdAsync(Guid id);
    Task AddAsync(SaleOrder saleOrder);
    Task UpdateAsync(SaleOrder saleOrder);
    Task DeleteAsync(SaleOrder saleOrder);
}
