using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlVendorDebtRepository : IVendorDebtRepository
{
    private readonly WmsDbContext _db;

    public SqlVendorDebtRepository(WmsDbContext db)
    {
        _db = db;
    }

    private IQueryable<VendorDebtLedger> QueryWithIncludes()
    {
        return _db.VendorDebtLedgers
            .Include(d => d.Vendor)
            .Include(d => d.SourceReceiving)
                .ThenInclude(r => r.PurchaseOrder)
            .Include(d => d.Product)
            .Include(d => d.ResolvedInPo);
    }

    public async Task<List<VendorDebtLedger>> GetAllAsync(
        Guid? vendorId = null,
        VendorDebtType? debtType = null,
        bool? isResolved = null)
    {
        var query = QueryWithIncludes().AsQueryable();

        if (vendorId.HasValue)
            query = query.Where(d => d.VendorId == vendorId.Value);

        if (debtType.HasValue)
            query = query.Where(d => d.DebtType == debtType.Value);

        if (isResolved.HasValue)
            query = query.Where(d => d.IsResolved == isResolved.Value);

        return await query
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<VendorDebtLedger>> GetUnresolvedByVendorIdAsync(Guid vendorId)
    {
        return await QueryWithIncludes()
            .Where(d => d.VendorId == vendorId && !d.IsResolved)
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<VendorDebtLedger>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return await QueryWithIncludes()
            .Where(d => idList.Contains(d.Id))
            .ToListAsync();
    }

    public async Task<List<VendorDebtLedger>> GetByResolvedInPoIdAsync(Guid poId)
    {
        return await QueryWithIncludes()
            .Where(d => d.ResolvedInPoId == poId)
            .ToListAsync();
    }

    public async Task<VendorDebtLedger?> GetByIdAsync(Guid id)
    {
        return await QueryWithIncludes()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddAsync(VendorDebtLedger debt)
    {
        await _db.VendorDebtLedgers.AddAsync(debt);
    }

    public async Task AddRangeAsync(IEnumerable<VendorDebtLedger> debts)
    {
        await _db.VendorDebtLedgers.AddRangeAsync(debts);
    }

    public Task UpdateAsync(VendorDebtLedger debt)
    {
        _db.VendorDebtLedgers.Update(debt);
        return Task.CompletedTask;
    }
}
