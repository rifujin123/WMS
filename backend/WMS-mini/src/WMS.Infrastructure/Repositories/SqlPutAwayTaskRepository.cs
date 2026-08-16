using Microsoft.EntityFrameworkCore;
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

    public async Task<List<PutAwayTask>> GetAllAsync()
    {
        return await _db.PutAwayTasks
            .Include(t => t.Product)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Include(t => t.AssignTo)
            .Include(t => t.ReceivingDetail)
            .ToListAsync();
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
