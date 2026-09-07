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

    public async Task<List<PickingDto>> GetAllAsync(Guid? assignToId = null) => _mapper.Map<List<PickingDto>>(await _repo.GetAllAsync(assignToId));

    public async Task<PickingDto?> GetByIdAsync(Guid id)
    {
        var picking = await _repo.GetByIdAsync(id);
        if (picking == null) return null;
        if (picking.AssignedToId != _currentUser.UserId && !_currentUser.IsInRole("Admin", "WarehouseManager"))
            return null;
        return _mapper.Map<PickingDto>(picking);
    }

    public async Task<PickingDto> CreateAsync(CreatePickingDto dto)
    {
        if (await _warehouseRepo.GetByIdAsync(dto.WarehouseId) == null) throw new InvalidOperationException("Không tìm thấy kho hàng.");
        var saleOrder = await _saleOrderRepo.GetByIdAsync(dto.SaleOrderId) ?? throw new InvalidOperationException("Không tìm thấy đơn bán hàng.");
        if (saleOrder.Status != SaleOrderStatus.New && saleOrder.Status != SaleOrderStatus.Allocated)
            throw new InvalidOperationException($"Không thể tạo phiếu lấy hàng cho đơn bán ở trạng thái '{saleOrder.Status}'. Đơn bán phải ở trạng thái 'Mới' (New) hoặc 'Đã phân bổ' (Allocated).");

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
                if (remaining > 0)
                {
                    var available = await _stockRepo.GetAvailableByProductAndWarehouseAsync(sod.ProductId, dto.WarehouseId);
                    var locations = available.Count > 0
                        ? $" Tồn khả dụng tại: {string.Join(", ", available.Select(s => $"{s.Location.Code} ({s.OnhandQty - s.ReservedQty})"))}."
                        : " Không có tồn kho trong kho này.";
                    throw new InvalidOperationException($"Không đủ tồn kho khả dụng cho sản phẩm '{sod.Product.Sku}'. Cần: {requiredQty}, Khả dụng: {requiredQty - remaining}.{locations}");
                }
                sod.Status = SaleOrderDetailStatus.Allocated;
            }
            if (picking.PickingDetails.Count == 0) throw new InvalidOperationException("Không có dòng sản phẩm nào có thể phân bổ trong đơn bán này.");

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
        if (picking.Status != PickingStatus.Open) throw new InvalidOperationException($"Không thể phân công phiếu lấy ở trạng thái '{picking.Status}'. Phiếu phải ở trạng thái 'Mở' (Open).");

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
        if (picking.Status != PickingStatus.Assigned) throw new InvalidOperationException($"Không thể bắt đầu phiếu lấy ở trạng thái '{picking.Status}'. Phiếu phải ở trạng thái 'Đã phân công' (Assigned).");
        if (picking.AssignedToId == null) throw new InvalidOperationException("Phiếu lấy hàng phải được phân công trước khi bắt đầu.");
        if (picking.AssignedToId != _currentUser.UserId && !_currentUser.IsInRole("Admin", "WarehouseManager"))
            throw new InvalidOperationException("Bạn chỉ có thể bắt đầu phiếu lấy hàng được phân công cho bạn.");

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
        if (picking.Status != PickingStatus.InProgress) throw new InvalidOperationException($"Không thể hoàn thành phiếu lấy ở trạng thái '{picking.Status}'. Phiếu phải ở trạng thái 'Đang xử lý' (InProgress).");
        if (picking.AssignedToId != _currentUser.UserId && !_currentUser.IsInRole("Admin", "WarehouseManager"))
            throw new InvalidOperationException("Bạn chỉ có thể hoàn thành phiếu lấy hàng được phân công cho bạn.");
        var byId = dto.Details.ToDictionary(d => d.DetailId);
        foreach (var detail in picking.PickingDetails)
        {
            if (!byId.TryGetValue(detail.Id, out var input)) throw new InvalidOperationException($"Thiếu số lượng đã lấy cho dòng sản phẩm.");
            if (input.QtyPicked != detail.QtyToPick) throw new InvalidOperationException($"Số lượng đã lấy phải khớp với số lượng yêu cầu của sản phẩm '{detail.Product.Sku}'. Cần lấy: {detail.QtyToPick}, Thực tế lấy: {input.QtyPicked}.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var sodIds = picking.PickingDetails.Where(d => d.SaleOrderDetailId != null).Select(d => d.SaleOrderDetailId!.Value).Distinct().ToList();
            var sodList = await _saleOrderRepo.GetDetailsWithOrdersByIdsAsync(sodIds);
            foreach (var detail in picking.PickingDetails)
            {
                if (detail.LocationId == null) throw new InvalidOperationException($"Cần có thông tin vị trí để hoàn thành dòng lấy hàng.");
                var input = byId[detail.Id];
                var stock = await _stockRepo.GetByProductAndLocationAsync(detail.ProductId, detail.LocationId.Value) ?? throw new InvalidOperationException("Không tìm thấy dữ liệu tồn kho tại vị trí lấy hàng.");
                if (stock.ReservedQty < input.QtyPicked || stock.OnhandQty < input.QtyPicked) throw new InvalidOperationException($"Không đủ tồn kho cho sản phẩm '{detail.Product.Sku}' tại vị trí '{stock.Location.Code}'.");
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
        if (picking.Status != PickingStatus.Open) throw new InvalidOperationException($"Không thể xóa phiếu lấy hàng ở trạng thái '{picking.Status}'. Chỉ có thể xóa phiếu 'Mở' (Open).");

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
