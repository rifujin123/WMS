using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services;

public class PickingService : IPickingService
{
    private readonly IPickingRepository _repo;
    private readonly ISaleOrderRepository _saleOrderRepo;
    private readonly IStockRepository _stockRepo;
    private readonly IStockMovementRepository _movementRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public PickingService(
        IPickingRepository repo, 
        ISaleOrderRepository saleOrderRepo, 
        IStockRepository stockRepo, 
        IStockMovementRepository movementRepo, 
        IWarehouseRepository warehouseRepo, 
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUser, 
        IMapper mapper)
    {
        _repo = repo;
        _saleOrderRepo = saleOrderRepo;
        _stockRepo = stockRepo;
        _movementRepo = movementRepo;
        _warehouseRepo = warehouseRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<List<PickingDto>> GetAllAsync() => _mapper.Map<List<PickingDto>>(await _repo.GetAllAsync());

    public async Task<PickingDto?> GetByIdAsync(Guid id)
    {
        var picking = await _repo.GetByIdAsync(id);
        return picking == null ? null : _mapper.Map<PickingDto>(picking);
    }

    public async Task<PickingDto> CreateAsync(CreatePickingDto dto)
    {
        if (await _warehouseRepo.GetByIdAsync(dto.WarehouseId) == null) throw new InvalidOperationException("Warehouse not found.");
        var saleOrder = await _saleOrderRepo.GetByIdAsync(dto.SaleOrderId) ?? throw new InvalidOperationException("SaleOrder not found.");
        if (saleOrder.Status != SaleOrderStatus.New && saleOrder.Status != SaleOrderStatus.Allocated)
            throw new InvalidOperationException($"Cannot create picking for SaleOrder in '{saleOrder.Status}' status. Must be 'New' or 'Allocated'.");

        var now = DateTime.UtcNow;
        var picking = new Picking { PickingNo = $"PICK-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..8]}", WarehouseId = dto.WarehouseId, Status = PickingStatus.Open, CreatedDate = now };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var sod in saleOrder.SaleOrderDetails)
            {
                var remaining = sod.Quantity - sod.AllocatedQty;
                if (remaining <= 0) continue;
                var requiredQty = remaining;
                foreach (var stock in await _stockRepo.GetAvailableByProductAndWarehouseAsync(sod.ProductId, dto.WarehouseId))
                {
                    var qtyToAllocate = Math.Min(remaining, stock.OnhandQty - stock.ReservedQty);
                    if (qtyToAllocate <= 0) continue;
                    stock.ReservedQty += qtyToAllocate;
                    await _stockRepo.UpdateAsync(stock);
                    sod.AllocatedQty += qtyToAllocate;
                    remaining -= qtyToAllocate;
                    picking.PickingDetails.Add(new PickingDetail { SaleOrderDetailId = sod.Id, ProductId = sod.ProductId, LocationId = stock.LocationId, QtyToPick = qtyToAllocate, QtyPicked = 0, Status = PickingDetailStatus.Pending, CreatedDate = now });
                    if (remaining == 0) break;
                }
                if (remaining > 0) throw new InvalidOperationException($"Insufficient stock for product '{sod.Product.Sku}'. Required: {requiredQty}, Available: {requiredQty - remaining}.");
                sod.Status = SaleOrderDetailStatus.Allocated;
            }
            if (picking.PickingDetails.Count == 0) throw new InvalidOperationException("No allocatable lines in this SaleOrder.");

            saleOrder.Status = SaleOrderStatus.Picking;
            await _saleOrderRepo.UpdateAsync(saleOrder);
            await _repo.AddAsync(picking);
            await _unitOfWork.SaveChangesAsync();
        });

        return (await GetByIdAsync(picking.Id))!;
    }

    public async Task<PickingDto?> AssignAsync(Guid id, Guid assignedToId)
    {
        var picking = await _repo.GetByIdAsync(id);
        if (picking == null) return null;
        if (picking.Status != PickingStatus.Open) throw new InvalidOperationException($"Cannot assign picking in '{picking.Status}' status. Must be 'Open'.");

        picking.AssignedToId = assignedToId;
        picking.AssignedById = _currentUser.UserId;
        picking.AssignedDate = DateTime.UtcNow;
        picking.Status = PickingStatus.Assigned;
        await _repo.UpdateAsync(picking);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PickingDto>(picking);
    }

    public async Task<PickingDto?> StartProgressAsync(Guid id)
    {
        var picking = await _repo.GetByIdAsync(id);
        if (picking == null) return null;
        if (picking.Status != PickingStatus.Assigned) throw new InvalidOperationException($"Cannot start picking in '{picking.Status}' status. Must be 'Assigned'.");
        if (picking.AssignedToId == null) throw new InvalidOperationException("Picking must be assigned before starting.");

        picking.Status = PickingStatus.InProgress;
        picking.StartedById = _currentUser.UserId;
        picking.StartedDate = DateTime.UtcNow;
        await _repo.UpdateAsync(picking);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PickingDto>(picking);
    }

    public async Task<PickingDto?> CompleteAsync(Guid id, CompletePickingDto dto)
    {
        var picking = await _repo.GetByIdAsync(id);
        if (picking == null) return null;
        if (picking.Status != PickingStatus.InProgress) throw new InvalidOperationException($"Cannot complete picking in '{picking.Status}' status. Must be 'InProgress'.");
        var byId = dto.Details.ToDictionary(d => d.DetailId);
        foreach (var detail in picking.PickingDetails)
        {
            if (!byId.TryGetValue(detail.Id, out var input)) throw new InvalidOperationException($"Missing picked quantity for detail '{detail.Id}'.");
            if (input.QtyPicked != detail.QtyToPick) throw new InvalidOperationException($"Picked quantity must equal required quantity for product '{detail.Product.Sku}'. Required: {detail.QtyToPick}, Picked: {input.QtyPicked}.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var sodIds = picking.PickingDetails.Where(d => d.SaleOrderDetailId != null).Select(d => d.SaleOrderDetailId!.Value).Distinct().ToList();
            var sodList = await _saleOrderRepo.GetDetailsWithOrdersByIdsAsync(sodIds);
            foreach (var detail in picking.PickingDetails)
            {
                if (detail.LocationId == null) throw new InvalidOperationException($"Location is required to complete detail '{detail.Id}'.");
                var input = byId[detail.Id];
                var stock = await _stockRepo.GetByProductAndLocationAsync(detail.ProductId, detail.LocationId.Value) ?? throw new InvalidOperationException("Stock not found for picked location.");
                if (stock.ReservedQty < input.QtyPicked || stock.OnhandQty < input.QtyPicked) throw new InvalidOperationException($"Insufficient stock for product '{detail.Product.Sku}' at location '{stock.Location.Code}'.");
                stock.ReservedQty -= input.QtyPicked;
                stock.OnhandQty -= input.QtyPicked;
                await _stockRepo.UpdateAsync(stock);
                await _movementRepo.AddAsync(new StockMovement { ProductId = detail.ProductId, LocationId = detail.LocationId.Value, MovementType = MovementType.Out, Qty = input.QtyPicked, Notes = $"Picking completed. PickingNo: {picking.PickingNo}" });
                detail.QtyPicked = input.QtyPicked;
                detail.Status = PickingDetailStatus.Picked;
            }
            foreach (var sod in sodList)
                if (picking.PickingDetails.Where(d => d.SaleOrderDetailId == sod.Id).All(d => d.Status == PickingDetailStatus.Picked)) sod.Status = SaleOrderDetailStatus.Picked;
            foreach (var saleOrder in sodList.Select(s => s.SaleOrder).Distinct())
                if (saleOrder.SaleOrderDetails.All(d => d.Status == SaleOrderDetailStatus.Picked))
                {
                    saleOrder.Status = SaleOrderStatus.Packed;
                    saleOrder.PackedById = _currentUser.UserId;
                    saleOrder.PackedDate = DateTime.UtcNow;
                }
            picking.Status = PickingStatus.Completed;
            picking.CompletedById = _currentUser.UserId;
            picking.CompletedDate = DateTime.UtcNow;
            await _repo.UpdateAsync(picking);
            await _unitOfWork.SaveChangesAsync();
        });

        return _mapper.Map<PickingDto>(picking);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var picking = await _repo.GetByIdAsync(id);
        if (picking == null) return false;
        if (picking.Status != PickingStatus.Open) throw new InvalidOperationException($"Cannot delete picking in '{picking.Status}' status. Must be 'Open'.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var detail in picking.PickingDetails)
            {
                if (detail.LocationId != null && await _stockRepo.GetByProductAndLocationAsync(detail.ProductId, detail.LocationId.Value) is { } stock)
                {
                    stock.ReservedQty -= detail.QtyToPick;
                    await _stockRepo.UpdateAsync(stock);
                }
                if (detail.SaleOrderDetailId != null && await _saleOrderRepo.GetDetailByIdAsync(detail.SaleOrderDetailId.Value) is { } sod)
                {
                    sod.AllocatedQty -= detail.QtyToPick;
                    sod.Status = SaleOrderDetailStatus.Pending;
                }
            }
            var orderIds = await _saleOrderRepo.GetSaleOrderIdsByPickingsAsync(new List<Guid> { id });
            var otherPickingIds = await _repo.GetPickingIdsExceptAsync(id);
            var linkedOrderIds = await _saleOrderRepo.GetSaleOrderIdsByPickingsAsync(otherPickingIds);
            foreach (var order in await _saleOrderRepo.GetByIdsAsync(orderIds))
                if (!linkedOrderIds.Contains(order.Id) && order.Status == SaleOrderStatus.Picking) order.Status = SaleOrderStatus.Allocated;
            await _repo.DeleteAsync(picking);
            await _unitOfWork.SaveChangesAsync();
        });

        return true;
    }
}
