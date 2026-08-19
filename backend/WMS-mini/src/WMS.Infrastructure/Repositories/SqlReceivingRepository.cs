using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlReceivingRepository : IReceivingRepository
{
    private readonly WmsDbContext _db;

    public SqlReceivingRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Receiving>> GetAllAsync()
    {
        return await _db.Receivings
            .Include(r => r.PurchaseOrder)
            .Include(r => r.ReceivingDetails)
                .ThenInclude(d => d.Product)
            .ToListAsync();
    }

    public async Task<PagedResult<ReceivingDto>> GetPagedAsync(
        ReceivingListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var receivings = _db.Receivings.AsNoTracking().AsQueryable();
        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            receivings = receivings.Where(r => r.ReceivingNo.Contains(search)
                || (r.PurchaseOrder.PoNumber != null && r.PurchaseOrder.PoNumber.Contains(search))
                || (r.ReceivedBy != null && r.ReceivedBy.FullName.Contains(search)));
        if (query.Status.HasValue)
            receivings = receivings.Where(r => r.Status == query.Status.Value);

        var totalCount = await receivings.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await receivings
            .OrderByDescending(r => r.ReceivedDate)
            .ThenBy(r => r.ReceivingNo)
            .ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReceivingDto
            {
                Id = r.Id,
                ReceivingNo = r.ReceivingNo,
                PurchaseOrderId = r.PurchaseOrderId,
                PoNumber = r.PurchaseOrder.PoNumber,
                ReceivedById = r.ReceivedById,
                ReceivedByName = r.ReceivedBy == null ? null : r.ReceivedBy.FullName,
                ReceivedDate = r.ReceivedDate,
                Status = r.Status,
                Notes = r.Notes,
                InvoiceImageUrl = r.InvoiceImageUrl,
                CreatedDate = r.CreatedDate,
            })
            .ToListAsync(cancellationToken);

        return PagedResult<ReceivingDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<Receiving?> GetByIdAsync(Guid id)
    {
        return await _db.Receivings
            .Include(r => r.PurchaseOrder)
            .Include(r => r.ReceivingDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Receiving?> GetConfirmedByPurchaseOrderIdAsync(Guid purchaseOrderId)
    {
        return await _db.Receivings
            .FirstOrDefaultAsync(r => r.PurchaseOrderId == purchaseOrderId && r.Status == ReceivingStatus.Confirmed);
    }

    public async Task<ReceivingDetail?> GetDetailByIdAsync(Guid id)
    {
        return await _db.ReceivingDetails
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddAsync(Receiving receiving)
    {
        await _db.Receivings.AddAsync(receiving);
    }

    public async Task UpdateAsync(Receiving receiving)
    {
        _db.Receivings.Update(receiving);
    }

    public async Task DeleteAsync(Receiving receiving)
    {
        // Mark details as deleted too because the required FK uses NoAction.
        _db.ReceivingDetails.RemoveRange(receiving.ReceivingDetails);
        _db.Receivings.Remove(receiving);
    }
}
