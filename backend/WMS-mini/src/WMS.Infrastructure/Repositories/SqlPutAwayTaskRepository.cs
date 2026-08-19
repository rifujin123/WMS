using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlPutAwayTaskRepository : IPutAwayTaskRepository
{
    private readonly WmsDbContext _db;

    public SqlPutAwayTaskRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PutAwayTaskDto>> GetPagedAsync(
        PutAwayTaskListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var tasks = _db.PutAwayTasks.AsNoTracking().AsQueryable();
        if (query.AssignToId.HasValue)
            tasks = tasks.Where(t => t.AssignToId == query.AssignToId.Value);
        if (query.Status.HasValue)
            tasks = tasks.Where(t => t.Status == query.Status.Value);

        var totalCount = await tasks.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await tasks
            .OrderByDescending(t => t.CreatedDate)
            .ThenBy(t => t.Product.Sku)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new PutAwayTaskDto
            {
                Id = t.Id,
                ReceivingDetailId = t.ReceivingDetailId,
                ProductId = t.ProductId,
                ProductSku = t.Product.Sku,
                ProductName = t.Product.Name,
                Quantity = t.Quantity,
                FromLocationId = t.FromLocationId,
                FromLocationCode = t.FromLocation == null ? null : t.FromLocation.Code,
                ToLocationId = t.ToLocationId,
                ToLocationCode = t.ToLocation == null ? null : t.ToLocation.Code,
                Status = t.Status,
                AssignToId = t.AssignToId,
                AssignToName = t.AssignTo == null ? null : t.AssignTo.FullName,
                AssignToAvatarUrl = t.AssignTo == null ? null : t.AssignTo.AvatarUrl,
                CreatedDate = t.CreatedDate,
            })
            .ToListAsync(cancellationToken);

        return PagedResult<PutAwayTaskDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<List<PutAwayTask>> GetAllAsync(Guid? assignToId = null)
    {
        IQueryable<PutAwayTask> tasks = _db.PutAwayTasks
            .Include(t => t.Product)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Include(t => t.AssignTo)
            .Include(t => t.ReceivingDetail)
            .AsNoTracking();

        if (assignToId.HasValue)
            tasks = tasks.Where(t => t.AssignToId == assignToId.Value);

        return await tasks.ToListAsync();
    }

    public async Task<PutAwayTask?> GetByIdAsync(Guid id)
    {
        return await _db.PutAwayTasks
            .Include(t => t.Product)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Include(t => t.AssignTo)
            .Include(t => t.ReceivingDetail)
                .ThenInclude(d => d.Receiving)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddAsync(PutAwayTask putAwayTask)
    {
        await _db.PutAwayTasks.AddAsync(putAwayTask);
    }

    public async Task UpdateAsync(PutAwayTask putAwayTask)
    {
        _db.PutAwayTasks.Update(putAwayTask);
    }

    public async Task DeleteAsync(PutAwayTask putAwayTask)
    {
        _db.PutAwayTasks.Remove(putAwayTask);
    }

    public Task<int> GetIncompleteCountByPurchaseOrderAsync(Guid purchaseOrderId) =>
        _db.PutAwayTasks
            .Where(t => t.ReceivingDetail.Receiving.PurchaseOrderId == purchaseOrderId)
            .CountAsync(t => t.Status != PutAwayTaskStatus.Completed);
}
