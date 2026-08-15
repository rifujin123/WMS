using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class PickingService : IPickingService
{
    private readonly IPickingRepository _repo;
    private readonly ISaleOrderRepository _saleOrderRepo;
    private readonly IStockRepository _stockRepo;
    private readonly IStockMovementRepository _movementRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly WmsDbContext _db;
    private readonly IMapper _mapper;

    public PickingService(
        IPickingRepository repo,
        ISaleOrderRepository saleOrderRepo,
        IStockRepository stockRepo,
        IStockMovementRepository movementRepo,
        IWarehouseRepository warehouseRepo,
        WmsDbContext db,
        IMapper mapper)
    {
        _repo = repo;
        _saleOrderRepo = saleOrderRepo;
        _stockRepo = stockRepo;
        _movementRepo = movementRepo;
        _warehouseRepo = warehouseRepo;
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<PickingDto>> GetAllAsync()
    {
        var results = await _repo.GetAllAsync();
        return _mapper.Map<List<PickingDto>>(results);
    }

    public async Task<PickingDto?> GetByIdAsync(Guid id)
    {
        var result = await _repo.GetByIdAsync(id);
        if (result == null) return null;
        return _mapper.Map<PickingDto>(result);
    }

    public async Task<PickingDto> CreateAsync(CreatePickingDto dto, Guid userId)
    {
        var warehouse = await _warehouseRepo.GetByIdAsync(dto.WarehouseId);
        if (warehouse == null)
            throw new InvalidOperationException("Warehouse not found.");

        var saleOrder = await _saleOrderRepo.GetByIdAsync(dto.SaleOrderId);
        if (saleOrder == null)
            throw new InvalidOperationException("SaleOrder not found.");

        if (saleOrder.Status != SaleOrderStatus.New && saleOrder.Status != SaleOrderStatus.Allocated)
            throw new InvalidOperationException(
                $"Cannot create picking for SaleOrder in '{saleOrder.Status}' status. Must be 'New' or 'Allocated'.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var picking = new Picking
        {
            PickingNo = $"PICK-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..8]}",
            WarehouseId = dto.WarehouseId,
            Status = PickingStatus.Open,
            CreatedById = userId,
            CreatedDate = DateTime.UtcNow
        };

        foreach (var sod in saleOrder.SaleOrderDetails)
        {
            var requiredQty = sod.Quantity - sod.AllocatedQty;
            if (requiredQty <= 0) continue;

            var remaining = requiredQty;
            var stocks = await _stockRepo.GetAvailableByProductAndWarehouseAsync(
                sod.ProductId, dto.WarehouseId);

            foreach (var stock in stocks)
            {
                var qtyToAllocate = Math.Min(remaining, stock.OnhandQty - stock.ReservedQty);
                if (qtyToAllocate <= 0) continue;

                stock.ReservedQty += qtyToAllocate;
                await _stockRepo.UpdateAsync(stock);

                sod.AllocatedQty += qtyToAllocate;
                remaining -= qtyToAllocate;

                picking.PickingDetails.Add(new PickingDetail
                {
                    SaleOrderDetailId = sod.Id,
                    ProductId = sod.ProductId,
                    LocationId = stock.LocationId,
                    QtyToPick = qtyToAllocate,
                    QtyPicked = 0,
                    Status = PickingDetailStatus.Pending,
                    CreatedById = userId,
                    CreatedDate = DateTime.UtcNow
                });

                if (remaining == 0) break;
            }

            if (remaining > 0)
                throw new InvalidOperationException(
                    $"Insufficient stock for product '{sod.Product.Sku}'. " +
                    $"Required: {requiredQty}, Available: {requiredQty - remaining}.");

            sod.Status = SaleOrderDetailStatus.Allocated;
        }

        if (picking.PickingDetails.Count == 0)
            throw new InvalidOperationException("No allocatable lines in this SaleOrder.");

        saleOrder.Status = SaleOrderStatus.Picking;
        await _saleOrderRepo.UpdateAsync(saleOrder);
        await _repo.AddAsync(picking);

        await tx.CommitAsync();
        return (await GetByIdAsync(picking.Id))!;
    }

    public async Task<PickingDto?> AssignAsync(Guid id, Guid userId)
    {
        var picking = await _repo.GetByIdAsync(id);
        if (picking == null) return null;

        if (picking.Status != PickingStatus.Open)
            throw new InvalidOperationException($"Cannot assign picking in '{picking.Status}' status. Must be 'Open'.");

        picking.AssignedToId = userId;
        picking.Status = PickingStatus.Assigned;
        await _repo.UpdateAsync(picking);
        return _mapper.Map<PickingDto>(picking);
    }

    public async Task<PickingDto?> StartProgressAsync(Guid id)
    {
        var picking = await _repo.GetByIdAsync(id);
        if (picking == null) return null;

        if (picking.Status != PickingStatus.Assigned)
            throw new InvalidOperationException($"Cannot start picking in '{picking.Status}' status. Must be 'Assigned'.");

        if (picking.AssignedToId == null)
            throw new InvalidOperationException("Picking must be assigned before starting.");

        picking.Status = PickingStatus.InProgress;
        await _repo.UpdateAsync(picking);
        return _mapper.Map<PickingDto>(picking);
    }

    public async Task<PickingDto?> CompleteAsync(Guid id, CompletePickingDto dto)
    {
        var picking = await _repo.GetByIdAsync(id);
        if (picking == null) return null;

        if (picking.Status != PickingStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete picking in '{picking.Status}' status. Must be 'InProgress'.");

        var byId = dto.Details.ToDictionary(d => d.DetailId);

        foreach (var detail in picking.PickingDetails)
        {
            if (!byId.TryGetValue(detail.Id, out var input))
                throw new InvalidOperationException($"Missing picked quantity for detail '{detail.Id}'.");

            if (input.QtyPicked < detail.QtyToPick)
                throw new InvalidOperationException(
                    $"Insufficient picked quantity for product '{detail.Product.Sku}'. " +
                    $"Required: {detail.QtyToPick}, Picked: {input.QtyPicked}.");

            if (input.QtyPicked > detail.QtyToPick)
                throw new InvalidOperationException(
                    $"Picked quantity exceeds required quantity for product '{detail.Product.Sku}'. " +
                    $"Required: {detail.QtyToPick}, Picked: {input.QtyPicked}.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var sodIds = picking.PickingDetails
            .Where(d => d.SaleOrderDetailId != null)
            .Select(d => d.SaleOrderDetailId!.Value)
            .Distinct()
            .ToList();
        var sodList = await _db.SaleOrderDetails
            .Where(x => sodIds.Contains(x.Id))
            .Include(x => x.SaleOrder)
                .ThenInclude(o => o.SaleOrderDetails)
            .ToListAsync();

        foreach (var detail in picking.PickingDetails)
        {
            if (detail.LocationId == null)
                throw new InvalidOperationException($"Location is required to complete detail '{detail.Id}'.");

            var input = byId[detail.Id];
            var stock = await _stockRepo.GetByProductAndLocationAsync(detail.ProductId, detail.LocationId.Value);
            if (stock == null)
                throw new InvalidOperationException("Stock not found for picked location.");

            if (stock.ReservedQty < input.QtyPicked)
                throw new InvalidOperationException(
                    $"Insufficient reserved stock for product '{detail.Product.Sku}' at location '{stock.Location.Code}'.");

            if (stock.OnhandQty < input.QtyPicked)
                throw new InvalidOperationException(
                    $"Insufficient on-hand stock for product '{detail.Product.Sku}' at location '{stock.Location.Code}'.");

            stock.ReservedQty -= input.QtyPicked;
            stock.OnhandQty -= input.QtyPicked;
            await _stockRepo.UpdateAsync(stock);

            var movement = new StockMovement
            {
                ProductId = detail.ProductId,
                LocationId = detail.LocationId.Value,
                MovementType = MovementType.Out,
                Qty = input.QtyPicked,
                Notes = $"Picking completed. PickingNo: {picking.PickingNo}"
            };
            await _movementRepo.AddAsync(movement);

            detail.QtyPicked = input.QtyPicked;
            detail.Status = PickingDetailStatus.Picked;
        }

        foreach (var sod in sodList)
        {
            var details = picking.PickingDetails
                .Where(d => d.SaleOrderDetailId == sod.Id)
                .ToList();
            if (details.Count > 0 && details.All(d => d.Status == PickingDetailStatus.Picked))
                sod.Status = SaleOrderDetailStatus.Picked;
        }

        foreach (var saleOrder in sodList.Select(s => s.SaleOrder).Distinct())
        {
            if (saleOrder.SaleOrderDetails.All(d => d.Status == SaleOrderDetailStatus.Picked))
                saleOrder.Status = SaleOrderStatus.Packed;
        }

        picking.Status = PickingStatus.Completed;
        await _repo.UpdateAsync(picking);

        await tx.CommitAsync();
        return _mapper.Map<PickingDto>(picking);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var picking = await _repo.GetByIdAsync(id);
        if (picking == null) return false;

        if (picking.Status != PickingStatus.Open)
            throw new InvalidOperationException($"Cannot delete picking in '{picking.Status}' status. Must be 'Open'.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        // Trả lại ReservedQty cho stock và AllocatedQty cho từng dòng SaleOrder
        foreach (var detail in picking.PickingDetails)
        {
            if (detail.LocationId != null)
            {
                var stock = await _stockRepo.GetByProductAndLocationAsync(detail.ProductId, detail.LocationId.Value);
                if (stock != null)
                {
                    stock.ReservedQty -= detail.QtyToPick;
                    await _stockRepo.UpdateAsync(stock);
                }
            }

            if (detail.SaleOrderDetailId != null)
            {
                var sod = await _db.SaleOrderDetails.FindAsync(detail.SaleOrderDetailId.Value);
                if (sod != null)
                {
                    sod.AllocatedQty -= detail.QtyToPick;
                    sod.Status = SaleOrderDetailStatus.Pending;
                }
            }
        }

        await _db.SaveChangesAsync();

        // Nếu không còn picking nào trỏ vào SaleOrder đó thì đưa về 'Allocated'
        var orderIds = await _db.SaleOrderDetails
            .Where(d => d.PickingDetails.Any(p => p.PickingId == id))
            .Select(d => d.SaleOrderId)
            .Distinct()
            .ToListAsync();
        var otherPickingIds = await _db.Pickings
            .Where(p => p.Id != id)
            .Select(p => p.Id)
            .ToListAsync();
        var linkedOrderIds = await _db.SaleOrderDetails
            .Where(d => d.PickingDetails.Any(p => otherPickingIds.Contains(p.PickingId)))
            .Select(d => d.SaleOrderId)
            .Distinct()
            .ToListAsync();

        var orders = await _db.SaleOrders
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync();
        foreach (var order in orders)
        {
            if (!linkedOrderIds.Contains(order.Id) && order.Status == SaleOrderStatus.Picking)
                order.Status = SaleOrderStatus.Allocated;
        }

        await _repo.DeleteAsync(picking);
        await tx.CommitAsync();
        return true;
    }
}
