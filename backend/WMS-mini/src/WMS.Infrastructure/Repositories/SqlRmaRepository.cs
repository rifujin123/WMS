using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlRmaRepository : IRmaRepository
{
    private readonly WmsDbContext _db;

    public SqlRmaRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Rma>> GetAllAsync()
    {
        return await _db.Rmas.ToListAsync();
    }

    public async Task<Rma?> GetByIdAsync(Guid id)
    {
        return await _db.Rmas.FindAsync(id);
    }

    public async Task AddAsync(Rma rma)
    {
        await _db.Rmas.AddAsync(rma);
    }

    public async Task UpdateAsync(Rma rma)
    {
        _db.Rmas.Update(rma);
    }

    public async Task DeleteAsync(Rma rma)
    {
        _db.Rmas.Remove(rma);
    }
}
