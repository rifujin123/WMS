using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Services;

public class PutAwayService : IPutAwayService
{
    private readonly IPutAwayTaskRepository _repo;
    private readonly IReceivingRepository _receivingRepo;
    private readonly IStockRepository _stockRepo;
    private readonly IStockMovementRepository _movementRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IMapper _mapper;

    public PutAwayService(
        IPutAwayTaskRepository repo,
        IReceivingRepository receivingRepo,
        IStockRepository stockRepo,
        IStockMovementRepository movementRepo,
        ILocationRepository locationRepo,
        IMapper mapper)
    {
        _repo = repo;
        _receivingRepo = receivingRepo;
        _stockRepo = stockRepo;
        _movementRepo = movementRepo;
        _locationRepo = locationRepo;
        _mapper = mapper;
    }

    public async Task<List<PutAwayTaskDto>> GetAllAsync()
    {
        var tasks = await _repo.GetAllAsync();
        return _mapper.Map<List<PutAwayTaskDto>>(tasks);
    }

    public async Task<PutAwayTaskDto?> GetByIdAsync(Guid id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null)
            return null;

        return _mapper.Map<PutAwayTaskDto>(task);
    }

    public async Task<PutAwayTaskDto> CreateAsync(CreatePutAwayTaskDto dto)
    {
        // Kiểm tra dòng nhận hàng gốc tồn tại và không cất vượt số lượng đã nhận
        var detail = await _receivingRepo.GetDetailByIdAsync(dto.ReceivingDetailId);
        if (detail == null)
            throw new InvalidOperationException("ReceivingDetail not found.");

        if (dto.Quantity > detail.ActualQuantity)
            throw new InvalidOperationException(
                $"Cannot create putaway with quantity {dto.Quantity}. Max allowed: {detail.ActualQuantity}.");

        var task = _mapper.Map<PutAwayTask>(dto);
        task.Status = PutAwayTaskStatus.Open;

        await _repo.AddAsync(task);
        return _mapper.Map<PutAwayTaskDto>(task);
    }

    public async Task<PutAwayTaskDto?> UpdateAsync(Guid id, UpdatePutAwayTaskDto dto)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null)
            return null;

        if (task.Status != PutAwayTaskStatus.Open)
            throw new InvalidOperationException($"Cannot update task in '{task.Status}' status. Must be 'Open'.");

        var detail = await _receivingRepo.GetDetailByIdAsync(dto.ReceivingDetailId);
        if (detail == null)
            throw new InvalidOperationException("ReceivingDetail not found.");

        if (dto.Quantity > detail.ActualQuantity)
            throw new InvalidOperationException(
                $"Cannot update task with quantity {dto.Quantity}. Max allowed: {detail.ActualQuantity}.");

        _mapper.Map(dto, task);
        await _repo.UpdateAsync(task);
        return _mapper.Map<PutAwayTaskDto>(task);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null)
            return false;

        if (task.Status != PutAwayTaskStatus.Open)
            throw new InvalidOperationException($"Cannot delete task in '{task.Status}' status. Must be 'Open'.");

        await _repo.DeleteAsync(task);
        return true;
    }

    public async Task<PutAwayTaskDto?> AssignAsync(Guid id, Guid userId)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null)
            return null;

        if (task.Status != PutAwayTaskStatus.Open)
            throw new InvalidOperationException($"Cannot assign task in '{task.Status}' status. Must be 'Open'.");

        task.AssignToId = userId;
        task.Status = PutAwayTaskStatus.Assigned;
        await _repo.UpdateAsync(task);
        return _mapper.Map<PutAwayTaskDto>(task);
    }

    public async Task<PutAwayTaskDto?> StartProgressAsync(Guid id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null)
            return null;

        if (task.Status != PutAwayTaskStatus.Assigned)
            throw new InvalidOperationException($"Cannot start task in '{task.Status}' status. Must be 'Assigned'.");

        if (task.ToLocationId == null)
            throw new InvalidOperationException("ToLocation must be set before starting putaway.");

        // Kiểm tra sức chứa của vị trí đích
        var location = await _locationRepo.GetByIdAsync(task.ToLocationId.Value);
        if (location == null)
            throw new InvalidOperationException("Destination location not found.");

        if (location.CurrentQuantity + task.Quantity > location.MaxQuantity)
            throw new InvalidOperationException(
                $"Location '{location.Code}' does not have enough capacity. " +
                $"Available: {location.MaxQuantity - location.CurrentQuantity}, Required: {task.Quantity}.");

        task.Status = PutAwayTaskStatus.InProgress;
        await _repo.UpdateAsync(task);
        return _mapper.Map<PutAwayTaskDto>(task);
    }

    public async Task<PutAwayTaskDto?> CompleteAsync(Guid id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null)
            return null;

        if (task.Status != PutAwayTaskStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete task in '{task.Status}' status. Must be 'InProgress'.");

        if (task.ToLocationId == null)
            throw new InvalidOperationException("ToLocation must be set to complete putaway.");

        // Cập nhật hoặc tạo mới Stock
        var stock = await _stockRepo.GetByProductAndLocationAsync(task.ProductId, task.ToLocationId.Value);
        if (stock == null)
        {
            stock = new Stock
            {
                ProductId = task.ProductId,
                LocationId = task.ToLocationId.Value,
                OnhandQty = task.Quantity,
                ReservedQty = 0
            };
            await _stockRepo.AddAsync(stock);
        }
        else
        {
            stock.OnhandQty += task.Quantity;
            await _stockRepo.UpdateAsync(stock);
        }

        // Cập nhật số lượng tại vị trí
        var location = await _locationRepo.GetByIdAsync(task.ToLocationId.Value);
        if (location != null)
        {
            location.CurrentQuantity += task.Quantity;
            await _locationRepo.UpdateAsync(location);
        }

        // Ghi nhận StockMovement
        var movement = new StockMovement
        {
            ProductId = task.ProductId,
            LocationId = task.ToLocationId.Value,
            MovementType = MovementType.In,
            Qty = task.Quantity,
            Notes = $"PutAway completed. TaskId: {task.Id}"
        };
        await _movementRepo.AddAsync(movement);

        task.Status = PutAwayTaskStatus.Completed;
        await _repo.UpdateAsync(task);
        return _mapper.Map<PutAwayTaskDto>(task);
    }
}