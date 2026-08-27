using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlStockMovementRepository : IStockMovementRepository
{
    private readonly WmsDbContext _db;

    public SqlStockMovementRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<StockMovement>> GetAllAsync()
    {
        return await _db.StockMovements
            .Include(s => s.Product)
            .Include(s => s.Location)
            .Include(s => s.CreatedBy)
            .ToListAsync();
    }

    public async Task<StockMovement?> GetByIdAsync(Guid id)
    {
        return await _db.StockMovements
            .Include(s => s.Product)
            .Include(s => s.Location)
            .Include(s => s.CreatedBy)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<PagedResult<StockMovementDto>> GetPagedAsync(StockMovementQueryDto query, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<StockMovement> movements = _db.StockMovements.AsNoTracking();

        if (query.ProductId.HasValue)
            movements = movements.Where(s => s.ProductId == query.ProductId.Value);
        if (query.LocationId.HasValue)
            movements = movements.Where(s => s.LocationId == query.LocationId.Value);
        if (query.MovementType.HasValue)
            movements = movements.Where(s => s.MovementType == query.MovementType.Value);
        if (query.FromUtc.HasValue)
            movements = movements.Where(s => s.CreatedDate >= query.FromUtc.Value);
        if (query.ToUtc.HasValue)
            movements = movements.Where(s => s.CreatedDate <= query.ToUtc.Value);

        var totalCount = await movements.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await movements
            .OrderByDescending(s => s.CreatedDate)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StockMovementDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductSku = s.Product.Sku,
                ProductName = s.Product.Name,
                LocationId = s.LocationId,
                LocationCode = s.Location.Code,
                MovementType = s.MovementType,
                Qty = s.Qty,
                Notes = s.Notes,
                OccurredAtUtc = s.CreatedDate,
                ActorUserId = s.CreatedById,
                ActorDisplayName = s.CreatedBy != null ? s.CreatedBy.FullName : null,
                ActorAvatarUrl = s.CreatedBy != null ? s.CreatedBy.AvatarUrl : null,
            })
            .ToListAsync(cancellationToken);

        return PagedResult<StockMovementDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task AddAsync(StockMovement stockMovement)
    {
        await _db.StockMovements.AddAsync(stockMovement);
    }

    public async Task UpdateAsync(StockMovement stockMovement)
    {
        _db.StockMovements.Update(stockMovement);
    }

    public async Task DeleteAsync(StockMovement stockMovement)
    {
        _db.StockMovements.Remove(stockMovement);
    }
}
