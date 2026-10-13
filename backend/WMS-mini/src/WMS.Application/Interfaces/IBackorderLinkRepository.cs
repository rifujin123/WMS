using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IBackorderLinkRepository
{
    Task<BackorderLink?> GetByIdAsync(Guid id);
    Task<List<BackorderLink>> GetAllAsync(Guid? saleOrderId = null, Guid? purchaseOrderId = null, Guid? productId = null);
    Task<List<BackorderLink>> GetBySaleOrderDetailIdAsync(Guid saleOrderDetailId);
    Task<List<BackorderLink>> GetBySaleOrderDetailIdsAsync(List<Guid> saleOrderDetailIds);
    Task<List<BackorderLink>> GetByPurchaseOrderDetailIdAsync(Guid purchaseOrderDetailId);
    Task<List<BackorderLink>> GetByPurchaseOrderDetailIdsAsync(List<Guid> purchaseOrderDetailIds);
    Task<List<BackorderLink>> GetUnfulfilledByPurchaseOrderDetailIdAsync(Guid purchaseOrderDetailId);
    Task<List<BackorderLink>> GetUnfulfilledByPurchaseOrderIdAsync(Guid purchaseOrderId);
    Task AddAsync(BackorderLink link);
    Task UpdateAsync(BackorderLink link);
    Task DeleteAsync(BackorderLink link);
    Task DeleteRangeAsync(IEnumerable<BackorderLink> links);
}
