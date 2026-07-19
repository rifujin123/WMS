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
        return await _db.PutAwayTasks.ToListAsync();
    }

    public async Task<PutAwayTask?> GetByIdAsync(Guid id)
    {
        return await _db.PutAwayTasks.FindAsync(id);
    }

    public async Task AddAsync(PutAwayTask putAwayTask)
    {
        await _db.PutAwayTasks.AddAsync(putAwayTask);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(PutAwayTask putAwayTask)
    {
        _db.PutAwayTasks.Update(putAwayTask);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(PutAwayTask putAwayTask)
    {
        _db.PutAwayTasks.Remove(putAwayTask);
        await _db.SaveChangesAsync();
    }
}
