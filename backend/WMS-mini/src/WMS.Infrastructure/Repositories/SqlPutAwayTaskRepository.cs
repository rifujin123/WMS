using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
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
}
