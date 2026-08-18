using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Services;

public class ReceivingService : IReceivingService
{
    private readonly IReceivingRepository _repo;
    private readonly IPutAwayTaskRepository _putAwayRepo;
    private readonly IPurchaseOrderRepository _poRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public ReceivingService(IReceivingRepository repo, IPutAwayTaskRepository putAwayRepo, IPurchaseOrderRepository poRepo, IUnitOfWork unitOfWork, ICurrentUserService currentUser, IMapper mapper)
    {
        _repo = repo; _putAwayRepo = putAwayRepo; 
        _poRepo = poRepo; _unitOfWork = unitOfWork; 
        _currentUser = currentUser; 
        _mapper = mapper;
    }

    public async Task<List<ReceivingDto>> GetAllAsync() => _mapper.Map<List<ReceivingDto>>(await _repo.GetAllAsync());
    public async Task<ReceivingDto?> GetByIdAsync(Guid id) {
        var receiving = await _repo.GetByIdAsync(id);
        if(receiving == null)
            return null;
        return _mapper.Map<ReceivingDto>(receiving);
    }

    public async Task<ReceivingDto> CreateAsync(CreateReceivingDto dto)
    {
        var po = await GetPurchaseOrderAsync(dto);
        ValidateDetails(dto, po);
        var now = DateTime.UtcNow;
        var receiving = new Receiving
        {
            ReceivingNo = $"RC-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..8]}",
            PurchaseOrderId = dto.PurchaseOrderId, 
            ReceivedById = _currentUser.UserId, 
            ReceivedDate = now,
            Status = ReceivingStatus.Draft, 
            Notes = dto.Notes,
            ReceivingDetails = dto.Details.Select(d => new ReceivingDetail { ProductId = d.ProductId, ExpectedQuantity = d.ExpectedQuantity, ActualQuantity = d.ActualQuantity, Condition = d.Condition }).ToList()
        };
        await _repo.AddAsync(receiving);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<ReceivingDto>(receiving);
    }

    public async Task<ReceivingDto?> ConfirmAsync(Guid id)
    {
        var receiving = await _repo.GetByIdAsync(id);
        if (receiving == null) return null;
        if (receiving.Status != ReceivingStatus.Draft) throw new InvalidOperationException($"Cannot confirm receiving in '{receiving.Status}' status. Must be 'Draft'.");
        var po = await _poRepo.GetByIdAsync(receiving.PurchaseOrderId) ?? throw new InvalidOperationException("PurchaseOrder not found.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var detail in receiving.ReceivingDetails)
            {
                var poDetail = po.PurchaseOrderDetails.FirstOrDefault(d => d.ProductId == detail.ProductId) ?? throw new InvalidOperationException($"Product '{detail.ProductId}' is not in PurchaseOrder '{po.PoNumber}'.");
                if (detail.Condition == ProductCondition.Ok)
                {
                    if (poDetail.ReceivedQuantity + detail.ActualQuantity > poDetail.OrderedQuantity) throw new InvalidOperationException($"Cannot confirm: receiving {detail.ActualQuantity} would exceed ordered quantity {poDetail.OrderedQuantity} (already received {poDetail.ReceivedQuantity}).");
                    poDetail.ReceivedQuantity += detail.ActualQuantity;
                }
            }

            if (po.PurchaseOrderDetails.All(d => d.ReceivedQuantity >= d.OrderedQuantity)) po.Status = PurchaseOrderStatus.Received;
            receiving.Status = ReceivingStatus.Confirmed;
            receiving.ConfirmedById = _currentUser.UserId;
            receiving.ConfirmedDate = DateTime.UtcNow;
            foreach (var detail in receiving.ReceivingDetails.Where(d => d.Condition == ProductCondition.Ok))
                await _putAwayRepo.AddAsync(new PutAwayTask { ReceivingDetailId = detail.Id, ProductId = detail.ProductId, Quantity = detail.ActualQuantity, Status = PutAwayTaskStatus.Open });
            await _poRepo.UpdateAsync(po);
            await _repo.UpdateAsync(receiving);
            await _unitOfWork.SaveChangesAsync();
        });

        return _mapper.Map<ReceivingDto>(receiving);
    }

    public async Task<ReceivingDto?> UpdateAsync(Guid id, CreateReceivingDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return null;
        if (entity.Status != ReceivingStatus.Draft) throw new InvalidOperationException($"Cannot update receiving in '{entity.Status}' status. Must be 'Draft'.");
        var po = await GetPurchaseOrderAsync(dto);
        ValidateDetails(dto, po);
        entity.PurchaseOrderId = dto.PurchaseOrderId; entity.Notes = dto.Notes;
        entity.ReceivingDetails.Clear();
        entity.ReceivingDetails = dto.Details.Select(d => new ReceivingDetail { ReceivingId = entity.Id, ProductId = d.ProductId, ExpectedQuantity = d.ExpectedQuantity, ActualQuantity = d.ActualQuantity, Condition = d.Condition }).ToList();
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<ReceivingDto>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;
        if (entity.Status != ReceivingStatus.Draft) throw new InvalidOperationException($"Cannot delete receiving in '{entity.Status}' status. Must be 'Draft'.");
        await _repo.DeleteAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<PurchaseOrder> GetPurchaseOrderAsync(CreateReceivingDto dto) => await _poRepo.GetByIdAsync(dto.PurchaseOrderId) ?? throw new InvalidOperationException("PurchaseOrder not found.");
    private static void ValidateDetails(CreateReceivingDto dto, PurchaseOrder po)
    {
        foreach (var detail in dto.Details)
        {
            var poDetail = po.PurchaseOrderDetails.FirstOrDefault(d => d.ProductId == detail.ProductId) ?? throw new InvalidOperationException($"Product '{detail.ProductId}' is not in PurchaseOrder '{po.PoNumber}'.");
            if (detail.ActualQuantity + poDetail.ReceivedQuantity > poDetail.OrderedQuantity) 
                throw new InvalidOperationException($"Cannot receive {detail.ActualQuantity} of product '{detail.ProductId}'. Remaining: {poDetail.OrderedQuantity - poDetail.ReceivedQuantity}.");
        }
    }
}
