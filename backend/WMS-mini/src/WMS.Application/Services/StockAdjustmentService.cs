using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Services;

public class StockAdjustmentService : IStockAdjustmentService
{
    private readonly IStockAdjustmentRepository _repo;
    private readonly IStockRepository _stockRepo;
    private readonly IStockMovementRepository _movementRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IMapper _mapper;

    public StockAdjustmentService(
        IStockAdjustmentRepository repo,
        IStockRepository stockRepo,
        IStockMovementRepository movementRepo,
        ILocationRepository locationRepo,
        IMapper mapper)
    {
        _repo = repo;
        _stockRepo = stockRepo;
        _movementRepo = movementRepo;
        _locationRepo = locationRepo;
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

    public async Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentDto dto, Guid userId)
    {
        var adjustment = new StockAdjustment
        {
            AdjustmentNo = $"ADJ-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Status = StockAdjustmentStatus.Draft,
            Notes = dto.Notes,
            CreatedById = userId,
            CreatedDate = DateTime.UtcNow,
            Details = dto.Details.Select(d => new StockAdjustmentDetail
            {
                ProductId = d.ProductId,
                LocationId = d.LocationId,
                CountedQty = d.CountedQty
            }).ToList()
        };

        await _repo.AddAsync(adjustment);
        return _mapper.Map<StockAdjustmentDto>(adjustment);
    }

    public async Task<StockAdjustmentDto?> ApproveAsync(Guid id)
    {
        var adjustment = await _repo.GetByIdAsync(id);
        if (adjustment == null)
            return null;

        if (adjustment.Status != StockAdjustmentStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot approve adjustment in '{adjustment.Status}' status. Must be 'Draft'.");

        foreach (var detail in adjustment.Details)
        {
            var stock = await _stockRepo.GetByProductAndLocationAsync(detail.ProductId, detail.LocationId);
            int delta;
            if (stock == null)
            {
                delta = detail.CountedQty; // từ 0 → CountedQty
                stock = new Stock
                {
                    ProductId = detail.ProductId,
                    LocationId = detail.LocationId,
                    OnhandQty = detail.CountedQty,
                    ReservedQty = 0
                };
                await _stockRepo.AddAsync(stock); // lưu thẳng CountedQty
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
                location.CurrentQuantity += delta;
                await _locationRepo.UpdateAsync(location);
            }

            var movement = new StockMovement
            {
                ProductId = detail.ProductId,
                LocationId = detail.LocationId,
                MovementType = MovementType.Adjustment,
                Qty = delta,
                Notes = $"Stock adjustment. AdjustmentNo: {adjustment.AdjustmentNo}"
            };
            await _movementRepo.AddAsync(movement);
        }

        adjustment.Status = StockAdjustmentStatus.Approved;
        await _repo.UpdateAsync(adjustment);
        return _mapper.Map<StockAdjustmentDto>(adjustment);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var adjustment = await _repo.GetByIdAsync(id);
        if (adjustment == null)
            return false;

        if (adjustment.Status != StockAdjustmentStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot delete adjustment in '{adjustment.Status}' status. Must be 'Draft'.");

        await _repo.DeleteAsync(adjustment);
        return true;
    }
}