using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IPutAwayTaskRepository
{
    Task<List<PutAwayTask>> GetAllAsync();
    Task<PutAwayTask?> GetByIdAsync(Guid id);
    Task AddAsync(PutAwayTask putAwayTask);
    Task UpdateAsync(PutAwayTask putAwayTask);
    Task DeleteAsync(PutAwayTask putAwayTask);
    Task<int> GetIncompleteCountByPurchaseOrderAsync(Guid purchaseOrderId);
}
