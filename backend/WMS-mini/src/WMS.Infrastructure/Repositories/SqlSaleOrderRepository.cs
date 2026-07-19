using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlSaleOrderRepository : ISaleOrderRepository
{
    private readonly WmsDbContext _db;

    public SqlSaleOrderRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<SaleOrder>> GetAllAsync()
    {
        return await _db.SaleOrders.ToListAsync();
    }

    public async Task<SaleOrder?> GetByIdAsync(Guid id)
    {
        return await _db.SaleOrders.FindAsync(id);
    }

    public async Task AddAsync(SaleOrder saleOrder)
    {
        await _db.SaleOrders.AddAsync(saleOrder);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SaleOrder saleOrder)
    {
        _db.SaleOrders.Update(saleOrder);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(SaleOrder saleOrder)
    {
        _db.SaleOrders.Remove(saleOrder);
        await _db.SaveChangesAsync();
    }
}
