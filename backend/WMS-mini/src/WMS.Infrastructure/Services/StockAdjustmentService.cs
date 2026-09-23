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
    private readonly IProductRepository _productRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public StockAdjustmentService(
        IStockAdjustmentRepository repo,
        IStockRepository stockRepo,
        IStockMovementRepository movementRepo,
        IProductRepository productRepo,
        ILocationRepository locationRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _repo = repo;
        _stockRepo = stockRepo;
        _movementRepo = movementRepo;
        _productRepo = productRepo;
        _locationRepo = locationRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<List<StockAdjustmentDto>> GetAllAsync()
    {
        var results = await _repo.GetAllAsync();

        if (_currentUser.IsInRole("WarehouseStaff"))
        {
            var userId = _currentUser.UserId;
            results = userId.HasValue
                ? results.Where(adjustment => adjustment.CreatedById == userId.Value).ToList()
                : [];
        }

        var dtos = _mapper.Map<List<StockAdjustmentDto>>(results);
        await PopulateSystemAndDifferenceQuantitiesAsync(dtos);
        return dtos;
    }

    public async Task<StockAdjustmentDto?> GetByIdAsync(Guid id)
    {
        var result = await _repo.GetByIdAsync(id);
        if (result == null || !CanAccess(result))
        {
            return null;
        }

        var dto = _mapper.Map<StockAdjustmentDto>(result);
        await PopulateSystemAndDifferenceQuantitiesAsync(dto);
        return dto;
    }

    public async Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentDto dto)
    {
        await ValidateDetailsAsync(dto.Details);

        var entity = new StockAdjustment
        {
            // Timestamp plus a random suffix keeps adjustment numbers unique even for concurrent requests.
            AdjustmentNo = $"ADJ-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..42],
            Status = StockAdjustmentStatus.Draft,
            Notes = dto.Notes,
            Details = dto.Details
                .Select(detail => new StockAdjustmentDetail
                {
                    ProductId = detail.ProductId,
                    LocationId = detail.LocationId,
                    CountedQty = detail.CountedQty,
                })
                .ToList(),
        };

        await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        var resultDto = _mapper.Map<StockAdjustmentDto>(entity);
        await PopulateSystemAndDifferenceQuantitiesAsync(resultDto);
        return resultDto;
    }

    public async Task<StockAdjustmentDto?> ApproveAsync(Guid id)
    {
        var adjustment = await _repo.GetByIdAsync(id);
        if (adjustment == null)
        {
            return null;
        }

        if (adjustment.Status != StockAdjustmentStatus.Draft)
        {
            throw new InvalidOperationException("Ch\u1ec9 c\u00f3 th\u1ec3 duy\u1ec7t phi\u1ebfu \u0111i\u1ec1u ch\u1ec9nh \u1edf tr\u1ea1ng th\u00e1i Nh\u00e1p.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var approvalDetails = new List<ApprovalDetail>();

            // Validate every detail before mutating stock, location, movement, or adjustment status.
            foreach (var detail in adjustment.Details)
            {
                var product = await _productRepo.GetByIdAsync(detail.ProductId);
                if (product == null)
                {
                    throw new InvalidOperationException("S\u1ea3n ph\u1ea9m trong phi\u1ebfu \u0111i\u1ec1u ch\u1ec9nh kh\u00f4ng c\u00f2n h\u1ee3p l\u1ec7.");
                }

                var location = await _locationRepo.GetByIdAsync(detail.LocationId);
                if (location == null)
                {
                    throw new InvalidOperationException("V\u1ecb tr\u00ed \u0111i\u1ec1u ch\u1ec9nh kh\u00f4ng h\u1ee3p l\u1ec7.");
                }

                var stock = await _stockRepo.GetByProductAndLocationAsync(detail.ProductId, detail.LocationId);
                var currentOnhand = stock?.OnhandQty ?? 0;
                var reservedQty = stock?.ReservedQty ?? 0;
                var delta = detail.CountedQty - currentOnhand;

                if (detail.CountedQty < reservedQty)
                {
                    throw new InvalidOperationException(
                        $"S\u1ed1 ki\u1ec3m \u0111\u1ebfm cho s\u1ea3n ph\u1ea9m '{product.Sku}' t\u1ea1i v\u1ecb tr\u00ed '{location.Code}' kh\u00f4ng \u0111\u01b0\u1ee3c nh\u1ecf h\u01a1n s\u1ed1 l\u01b0\u1ee3ng \u0111\u00e3 gi\u1eef ch\u1ed7 ({reservedQty}).");
                }

                approvalDetails.Add(new ApprovalDetail(detail, stock, location, delta));
            }

            foreach (var locationGroup in approvalDetails.GroupBy(item => item.Location.Id))
            {
                var location = locationGroup.First().Location;
                var finalLocationQuantity = location.CurrentQuantity + locationGroup.Sum(item => item.Delta);

                if (finalLocationQuantity < 0)
                {
                    throw new InvalidOperationException($"\u0056\u1ecb tr\u00ed '{location.Code}' kh\u00f4ng th\u1ec3 c\u00f3 s\u1ed1 l\u01b0\u1ee3ng \u00e2m sau khi \u0111i\u1ec1u ch\u1ec9nh.");
                }

                if (finalLocationQuantity > location.MaxQuantity)
                {
                    throw new InvalidOperationException(
                        $"\u0056\u1ecb tr\u00ed '{location.Code}' v\u01b0\u1ee3t s\u1ee9c ch\u1ee9a t\u1ed1i \u0111a {location.MaxQuantity} sau khi \u0111i\u1ec1u ch\u1ec9nh.");
                }
            }

            foreach (var approvalDetail in approvalDetails)
            {
                if (approvalDetail.Stock == null)
                {
                    await _stockRepo.AddAsync(new Stock
                    {
                        ProductId = approvalDetail.Detail.ProductId,
                        LocationId = approvalDetail.Detail.LocationId,
                        OnhandQty = approvalDetail.Detail.CountedQty,
                        ReservedQty = 0,
                    });
                }
                else
                {
                    approvalDetail.Stock.OnhandQty = approvalDetail.Detail.CountedQty;
                    await _stockRepo.UpdateAsync(approvalDetail.Stock);
                }

                await _movementRepo.AddAsync(new StockMovement
                {
                    ProductId = approvalDetail.Detail.ProductId,
                    LocationId = approvalDetail.Detail.LocationId,
                    MovementType = MovementType.Adjustment,
                    Qty = approvalDetail.Delta,
                    Notes = $"Ki\u1ec3m k\u00ea t\u1ed3n kho. AdjustmentNo: {adjustment.AdjustmentNo}",
                });
            }

            foreach (var locationGroup in approvalDetails.GroupBy(item => item.Location.Id))
            {
                var location = locationGroup.First().Location;
                location.CurrentQuantity += locationGroup.Sum(item => item.Delta);
                await _locationRepo.UpdateAsync(location);
            }

            adjustment.Status = StockAdjustmentStatus.Approved;
            adjustment.ApprovedById = _currentUser.UserId;
            adjustment.ApprovedDate = DateTime.UtcNow;
            await _repo.UpdateAsync(adjustment);
            await _unitOfWork.SaveChangesAsync();
        });

        var dto = _mapper.Map<StockAdjustmentDto>(adjustment);
        await PopulateSystemAndDifferenceQuantitiesAsync(dto);
        return dto;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null)
        {
            return false;
        }

        if (entity.Status != StockAdjustmentStatus.Draft)
        {
            throw new InvalidOperationException("Ch\u1ec9 c\u00f3 th\u1ec3 x\u00f3a phi\u1ebfu \u0111i\u1ec1u ch\u1ec9nh \u1edf tr\u1ea1ng th\u00e1i Nh\u00e1p.");
        }

        await _repo.DeleteAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task ValidateDetailsAsync(IReadOnlyCollection<CreateStockAdjustmentDetailDto> details)
    {
        if (details.Count == 0)
        {
            throw new InvalidOperationException("Phi\u1ebfu \u0111i\u1ec1u ch\u1ec9nh ph\u1ea3i c\u00f3 \u00edt nh\u1ea5t m\u1ed9t d\u00f2ng chi ti\u1ebft.");
        }

        if (details.Any(detail => detail is null))
        {
            throw new InvalidOperationException("Chi\u1ebf t\u1ebft \u0111i\u1ec1u ch\u1ec9nh kh\u00f4ng \u0111\u01b0\u1ee3c \u0111\u1ec3 tr\u1ed1ng.");
        }

        var duplicateDetail = details
            .GroupBy(detail => new { detail.ProductId, detail.LocationId })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateDetail != null)
        {
            throw new InvalidOperationException("M\u1ed7i c\u1eb7p s\u1ea3n ph\u1ea9m v\u00e0 v\u1ecb tr\u00ed ch\u1ec9 \u0111\u01b0\u1ee3c xu\u1ea5t hi\u1ec7n m\u1ed9t l\u1ea7n trong phi\u1ebfu \u0111i\u1ec1u ch\u1ec9nh.");
        }

        foreach (var detail in details)
        {
            if (detail.ProductId == Guid.Empty || await _productRepo.GetByIdAsync(detail.ProductId) == null)
            {
                throw new InvalidOperationException("S\u1ea3n ph\u1ea9m \u0111i\u1ec1u ch\u1ec9nh kh\u00f4ng h\u1ee3p l\u1ec7.");
            }

            if (detail.LocationId == Guid.Empty || await _locationRepo.GetByIdAsync(detail.LocationId) == null)
            {
                throw new InvalidOperationException("V\u1ecb tr\u00ed \u0111i\u1ec1u ch\u1ec9nh kh\u00f4ng h\u1ee3p l\u1ec7.");
            }

            if (detail.CountedQty < 0)
            {
                throw new InvalidOperationException("S\u1ed1 l\u01b0\u1ee3ng ki\u1ec3m \u0111\u1ebfm kh\u00f4ng \u0111\u01b0\u1ee3c \u00e2m.");
            }
        }
    }

    private bool CanAccess(StockAdjustment adjustment)
    {
        if (!_currentUser.IsInRole("WarehouseStaff"))
        {
            return true;
        }

        var userId = _currentUser.UserId;
        return userId.HasValue && adjustment.CreatedById == userId.Value;
    }

    private async Task PopulateSystemAndDifferenceQuantitiesAsync(StockAdjustmentDto dto)
    {
        await PopulateSystemAndDifferenceQuantitiesAsync([dto]);
    }

    private async Task PopulateSystemAndDifferenceQuantitiesAsync(List<StockAdjustmentDto> dtos)
    {
        if (dtos.Count == 0) return;

        // Lấy toàn bộ tồn kho để tra cứu nhanh số lượng tồn trên hệ thống cho các phiếu Draft
        var allStocks = await _stockRepo.GetAllAsync();
        var stockLookup = allStocks
            .GroupBy(s => (s.ProductId, s.LocationId))
            .ToDictionary(g => g.Key, g => g.First());

        // Lấy danh sách số phiếu của các phiếu đã duyệt
        var approvedNos = dtos
            .Where(d => d.Status == StockAdjustmentStatus.Approved)
            .Select(d => d.AdjustmentNo)
            .ToHashSet();

        List<StockMovement> movements = [];
        if (approvedNos.Count > 0)
        {
            var allMovements = await _movementRepo.GetAllAsync();
            movements = allMovements
                .Where(m => m.MovementType == MovementType.Adjustment && m.Notes != null && approvedNos.Any(no => m.Notes.Contains(no)))
                .ToList();
        }

        foreach (var dto in dtos)
        {
            if (dto.Status == StockAdjustmentStatus.Draft)
            {
                foreach (var detail in dto.Details)
                {
                    detail.SystemQty = stockLookup.TryGetValue((detail.ProductId, detail.LocationId), out var stock)
                        ? stock.OnhandQty
                        : 0;
                }
            }
            else // Approved
            {
                foreach (var detail in dto.Details)
                {
                    var movement = movements.FirstOrDefault(m =>
                        m.ProductId == detail.ProductId &&
                        m.LocationId == detail.LocationId &&
                        m.Notes != null &&
                        m.Notes.Contains(dto.AdjustmentNo));

                    detail.SystemQty = movement != null
                        ? detail.CountedQty - movement.Qty
                        : detail.CountedQty;
                }
            }
        }
    }

    private sealed record ApprovalDetail(
        StockAdjustmentDetail Detail,
        Stock? Stock,
        Location Location,
        int Delta);
}
