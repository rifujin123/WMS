using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IPutAwayTaskRepository
{
    Task<List<PutAwayTask>> GetAllAsync(Guid? assignToId = null);
    Task<PagedResult<PutAwayTaskDto>> GetPagedAsync(PutAwayTaskListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<PutAwayTask?> GetByIdAsync(Guid id);
    Task AddAsync(PutAwayTask putAwayTask);
    Task UpdateAsync(PutAwayTask putAwayTask);
    Task DeleteAsync(PutAwayTask putAwayTask);
    Task<int> GetIncompleteCountByPurchaseOrderAsync(Guid purchaseOrderId);
}
