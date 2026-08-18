using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services;

public class StockAdjustmentService : IStockAdjustmentService
{
    private readonly IStockAdjustmentRepository _repo;
    private readonly IStockRepository _stockRepo;
    private readonly IStockMovementRepository _movementRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public StockAdjustmentService(
        IStockAdjustmentRepository repo, 
        IStockRepository stockRepo, 
        IStockMovementRepository movementRepo, 
        ILocationRepository locationRepo, 
        IUnitOfWork unitOfWork, ICurrentUserService currentUser, IMapper mapper)
    {
        _repo = repo; 
        _stockRepo = stockRepo; 
        _movementRepo = movementRepo; 
        _locationRepo = locationRepo; 
        _unitOfWork = unitOfWork; 
        _currentUser = currentUser; 
        _mapper = mapper;
    }

    public async Task<List<StockAdjustmentDto>> GetAllAsync()
    {
        var results = await _repo.GetAllAsync();
        return _mapper.Map<List<StockAdjustmentDto>>(results);
    }

    public async Task<StockAdjustmentDto?> GetByIdAsync(Guid id)
    {
        var result = await _repo.GetByIdAsync(id);
        if (result == null) return null;
        return _mapper.Map<StockAdjustmentDto>(result);
    }

    public async Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentDto dto)
    {
        var entity = new StockAdjustment
        {
            AdjustmentNo = $"ADJ-{DateTime.UtcNow:yyyyMMddHHmmss}", 
            Status = StockAdjustmentStatus.Draft, 
            Notes = dto.Notes,
            Details = dto.Details.Select(d => new StockAdjustmentDetail { ProductId = d.ProductId, LocationId = d.LocationId, CountedQty = d.CountedQty }).ToList()
        };
        await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<StockAdjustmentDto>(entity);
    }

    public async Task<StockAdjustmentDto?> ApproveAsync(Guid id)
    {
        var adjustment = await _repo.GetByIdAsync(id);
        if (adjustment == null) return null;
        if (adjustment.Status != StockAdjustmentStatus.Draft) throw new InvalidOperationException($"Cannot approve adjustment in '{adjustment.Status}' status. Must be 'Draft'.");
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var detail in adjustment.Details)
            {
                var stock = await _stockRepo.GetByProductAndLocationAsync(detail.ProductId, detail.LocationId);
                var delta = detail.CountedQty;
                if (stock == null)
                {
                    stock = new Stock { ProductId = detail.ProductId, LocationId = detail.LocationId, OnhandQty = detail.CountedQty, ReservedQty = 0 };
                    await _stockRepo.AddAsync(stock);
                }
                else
                {
                    delta = detail.CountedQty - stock.OnhandQty;
                    stock.OnhandQty = detail.CountedQty;
                    await _stockRepo.UpdateAsync(stock);
                }
                var location = await _locationRepo.GetByIdAsync(detail.LocationId);
                if (location != null)
                {
                    if (location.CurrentQuantity + delta > location.MaxQuantity) throw new InvalidOperationException($"Location '{location.Code}' does not have enough capacity. Available: {location.MaxQuantity - location.CurrentQuantity}, Adjustment delta: {delta}.");
                    location.CurrentQuantity += delta;
                    await _locationRepo.UpdateAsync(location);
                }
                await _movementRepo.AddAsync(new StockMovement { ProductId = detail.ProductId, LocationId = detail.LocationId, MovementType = MovementType.Adjustment, Qty = delta, Notes = $"Stock adjustment. AdjustmentNo: {adjustment.AdjustmentNo}" });
            }
            adjustment.Status = StockAdjustmentStatus.Approved;
            adjustment.ApprovedById = _currentUser.UserId;
            adjustment.ApprovedDate = DateTime.UtcNow;
            await _repo.UpdateAsync(adjustment);
            await _unitOfWork.SaveChangesAsync();
        });

        return _mapper.Map<StockAdjustmentDto>(adjustment);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;
        if (entity.Status != StockAdjustmentStatus.Draft) throw new InvalidOperationException($"Cannot delete adjustment in '{entity.Status}' status. Must be 'Draft'.");
        await _repo.DeleteAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
