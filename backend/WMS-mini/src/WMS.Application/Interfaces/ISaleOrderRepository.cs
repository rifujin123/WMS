using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface ISaleOrderRepository
{
    Task<List<SaleOrder>> GetAllAsync();
    Task<SaleOrder?> GetByIdAsync(Guid id);
    Task<SaleOrder?> GetByOrderNoAsync(string orderNo);
    Task AddAsync(SaleOrder saleOrder);
    Task UpdateAsync(SaleOrder saleOrder);
    Task DeleteAsync(SaleOrder saleOrder);
    Task RemoveDetailsAsync(Guid saleOrderId);
    Task<SaleOrderDetail?> GetDetailByIdAsync(Guid detailId);
    Task<List<SaleOrderDetail>> GetDetailsWithOrdersByIdsAsync(List<Guid> detailIds);
    Task<List<Guid>> GetSaleOrderIdsByPickingsAsync(List<Guid> pickingIds);
    Task<List<SaleOrder>> GetByIdsAsync(List<Guid> ids);
}
