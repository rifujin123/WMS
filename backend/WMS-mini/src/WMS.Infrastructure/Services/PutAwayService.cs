using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services;

public class PutAwayService : IPutAwayService
{
    private readonly IPutAwayTaskRepository _repo;
    private readonly IReceivingRepository _receivingRepo;
    private readonly IPurchaseOrderRepository _poRepo;
    private readonly IStockRepository _stockRepo;
    private readonly IStockMovementRepository _movementRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public PutAwayService(
        IPutAwayTaskRepository repo, 
        IReceivingRepository receivingRepo, 
        IPurchaseOrderRepository poRepo,
        IStockRepository stockRepo, 
        IStockMovementRepository movementRepo, 
        ILocationRepository locationRepo, 
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUser, 
        IMapper mapper)
    {
        _repo = repo;
        _receivingRepo = receivingRepo;
        _poRepo = poRepo;
        _stockRepo = stockRepo;
        _movementRepo = movementRepo;
        _locationRepo = locationRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<List<PutAwayTaskDto>> GetAllAsync(Guid? assignToId = null)
    {
        var tasks = await _repo.GetAllAsync(assignToId);
        return _mapper.Map<List<PutAwayTaskDto>>(tasks);
    }

    public Task<PagedResult<PutAwayTaskDto>> GetPagedAsync(
        PutAwayTaskListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _repo.GetPagedAsync(query, pageSize, cancellationToken);

    public async Task<PutAwayTaskDto?> GetByIdAsync(Guid id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null)
            return null;

        if (task.AssignToId != _currentUser.UserId && !_currentUser.IsInRole("Admin", "WarehouseManager"))
            return null;

        return _mapper.Map<PutAwayTaskDto>(task);
    }

    public async Task<PutAwayTaskDto> CreateAsync(CreatePutAwayTaskDto dto)
    {
        var detail = await _receivingRepo.GetDetailByIdAsync(dto.ReceivingDetailId) ?? throw new InvalidOperationException("Không tìm thấy chi tiết phiếu nhận.");
        if (dto.Quantity > detail.ActualQuantity)
            throw new InvalidOperationException($"Không thể tạo task cất hàng với số lượng {dto.Quantity}. Số lượng tối đa cho phép là {detail.ActualQuantity}.");

        var task = _mapper.Map<PutAwayTask>(dto);
        task.Status = PutAwayTaskStatus.Open;
        await _repo.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PutAwayTaskDto>(task);
    }

    public async Task<PutAwayTaskDto?> UpdateAsync(Guid id, UpdatePutAwayTaskDto dto)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return null;
        if (task.Status != PutAwayTaskStatus.Open && task.Status != PutAwayTaskStatus.Assigned)
            throw new InvalidOperationException($"Không thể cập nhật task ở trạng thái '{task.Status}'. Chỉ cho phép cập nhật khi task ở trạng thái 'Mở' (Open) hoặc 'Đã phân công' (Assigned).");

        var detail = await _receivingRepo.GetDetailByIdAsync(dto.ReceivingDetailId) ?? throw new InvalidOperationException("Không tìm thấy chi tiết phiếu nhận.");
        if (dto.Quantity > detail.ActualQuantity)
            throw new InvalidOperationException($"Không thể cập nhật task với số lượng {dto.Quantity}. Số lượng tối đa cho phép là {detail.ActualQuantity}.");

        _mapper.Map(dto, task);
        await _repo.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PutAwayTaskDto>(task);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return false;
        if (task.Status != PutAwayTaskStatus.Open)
            throw new InvalidOperationException($"Không thể xóa task ở trạng thái '{task.Status}'. Chỉ có thể xóa khi task ở trạng thái 'Mở' (Open).");

        await _repo.DeleteAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<PutAwayTaskDto?> AssignAsync(Guid id, Guid assignedToId)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return null;
        if (task.Status != PutAwayTaskStatus.Open)
            throw new InvalidOperationException($"Không thể phân công task ở trạng thái '{task.Status}'. Chỉ có thể phân công khi task ở trạng thái 'Mở' (Open).");

        task.AssignToId = assignedToId;
        task.AssignedById = _currentUser.UserId;
        task.AssignedDate = DateTime.UtcNow;
        task.Status = PutAwayTaskStatus.Assigned;
        await _repo.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PutAwayTaskDto>(task);
    }

    public async Task<PutAwayTaskDto?> StartProgressAsync(Guid id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return null;
        if (task.Status != PutAwayTaskStatus.Assigned)
            throw new InvalidOperationException($"Không thể bắt đầu task ở trạng thái '{task.Status}'. Task phải ở trạng thái 'Đã phân công' (Assigned).");
        if (task.AssignToId != _currentUser.UserId && !_currentUser.IsInRole("Admin", "WarehouseManager"))
            throw new InvalidOperationException("Bạn chỉ có thể bắt đầu task được phân công cho bạn.");
        if (task.ToLocationId == null)
            throw new InvalidOperationException("Phải thiết lập vị trí đích (Location) trước khi bắt đầu cất hàng.");

        var location = await _locationRepo.GetByIdAsync(task.ToLocationId.Value) ?? throw new InvalidOperationException("Không tìm thấy vị trí đích trong kho.");
        if (location.CurrentQuantity + task.Quantity > location.MaxQuantity)
            throw new InvalidOperationException($"Vị trí '{location.Code}' không đủ sức chứa. Còn trống: {location.MaxQuantity - location.CurrentQuantity}, Yêu cầu cất: {task.Quantity}.");

        task.Status = PutAwayTaskStatus.InProgress;
        task.StartedById = _currentUser.UserId;
        task.StartedDate = DateTime.UtcNow;
        await _repo.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PutAwayTaskDto>(task);
    }

    public async Task<PutAwayTaskDto?> CompleteAsync(Guid id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return null;
        if (task.Status != PutAwayTaskStatus.InProgress)
            throw new InvalidOperationException($"Không thể hoàn thành task ở trạng thái '{task.Status}'. Task phải ở trạng thái 'Đang xử lý' (InProgress).");
        if (task.AssignToId != _currentUser.UserId && !_currentUser.IsInRole("Admin", "WarehouseManager"))
            throw new InvalidOperationException("Bạn chỉ có thể hoàn thành task được phân công cho bạn.");
        if (task.ToLocationId == null)
            throw new InvalidOperationException("Phải thiết lập vị trí đích (Location) trước khi hoàn thành cất hàng.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var location = await _locationRepo.GetByIdAsync(task.ToLocationId.Value) ?? throw new InvalidOperationException("Không tìm thấy vị trí đích trong kho.");
            if (location.CurrentQuantity + task.Quantity > location.MaxQuantity)
                throw new InvalidOperationException($"Vị trí '{location.Code}' không đủ sức chứa. Còn trống: {location.MaxQuantity - location.CurrentQuantity}, Yêu cầu cất: {task.Quantity}.");

            var stock = await _stockRepo.GetByProductAndLocationAsync(task.ProductId, task.ToLocationId.Value);
            if (stock == null)
            {
                await _stockRepo.AddAsync(new Stock { ProductId = task.ProductId, LocationId = task.ToLocationId.Value, OnhandQty = task.Quantity, ReservedQty = 0 });
            }
            else
            {
                stock.OnhandQty += task.Quantity;
                await _stockRepo.UpdateAsync(stock);
            }

            location.CurrentQuantity += task.Quantity;
            await _locationRepo.UpdateAsync(location);
            await _movementRepo.AddAsync(new StockMovement { ProductId = task.ProductId, LocationId = task.ToLocationId.Value, MovementType = MovementType.In, Qty = task.Quantity, Notes = $"PutAway completed. TaskId: {task.Id}" });
            task.Status = PutAwayTaskStatus.Completed;
            task.CompletedById = _currentUser.UserId;
            task.CompletedDate = DateTime.UtcNow;
            await _repo.UpdateAsync(task);
            await _unitOfWork.SaveChangesAsync();

            // Auto-close the PO when every put-away task of the purchase order is completed.
            var purchaseOrderId = task.ReceivingDetail.Receiving?.PurchaseOrderId;
            if (purchaseOrderId != null)
            {
                var incompleteCount = await _repo.GetIncompleteCountByPurchaseOrderAsync(purchaseOrderId.Value);
                if (incompleteCount == 0)
                {
                    var po = await _poRepo.GetByIdAsync(purchaseOrderId.Value);
                    if (po != null && po.Status == PurchaseOrderStatus.Received)
                    {
                        po.Status = PurchaseOrderStatus.Closed;
                        po.ClosedById = _currentUser.UserId;
                        po.ClosedDate = DateTime.UtcNow;
                        await _poRepo.UpdateAsync(po);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }
            }
        });

        return _mapper.Map<PutAwayTaskDto>(task);
    }
}
