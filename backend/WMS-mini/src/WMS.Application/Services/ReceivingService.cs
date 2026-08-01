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
    private readonly IMapper _mapper;

    public ReceivingService(
        IReceivingRepository repo,
        IPutAwayTaskRepository putAwayRepo,
        IPurchaseOrderRepository poRepo,
        IMapper mapper)
    {
        _repo = repo;
        _putAwayRepo = putAwayRepo;
        _poRepo = poRepo;
        _mapper = mapper;
    }

    public async Task<List<ReceivingDto>> GetAllAsync()
    {
        var results = await _repo.GetAllAsync();
        return _mapper.Map<List<ReceivingDto>>(results);
    }

    public async Task<ReceivingDto?> GetByIdAsync(Guid id)
    {
        var result = await _repo.GetByIdAsync(id);
        if (result == null) return null;
        return _mapper.Map<ReceivingDto>(result);
    }

    public async Task<ReceivingDto> CreateAsync(CreateReceivingDto dto, Guid userId)
    {
        var po = await _poRepo.GetByIdAsync(dto.PurchaseOrderId);
        if (po == null)
            throw new InvalidOperationException("PurchaseOrder not found.");

        var validProductIds = po.PurchaseOrderDetails
            .Select(d => d.ProductId)
            .ToList();

        foreach (var detail in dto.Details)
        {
            if (!validProductIds.Contains(detail.ProductId))
                throw new InvalidOperationException(
                    $"Product '{detail.ProductId}' is not in PurchaseOrder '{po.PoNumber}'.");
        }

        var receiving = new Receiving
        {
            PurchaseOrderId = dto.PurchaseOrderId,
            ReceivedById = userId,
            ReceivedDate = DateTime.UtcNow,
            Status = ReceivingStatus.Draft,
            Notes = dto.Notes
        };

        receiving.ReceivingDetails = dto.Details.Select(d => new ReceivingDetail
        {
            ProductId = d.ProductId,
            ExpectedQuantity = d.ExpectedQuantity,
            ActualQuantity = d.ActualQuantity,
            Condition = d.Condition
        }).ToList();

        await _repo.AddAsync(receiving);
        return _mapper.Map<ReceivingDto>(receiving);
    }

    public async Task<ReceivingDto?> ConfirmAsync(Guid id)
    {
        var receiving = await _repo.GetByIdAsync(id);
        if (receiving == null)
            return null;

        if (receiving.Status != ReceivingStatus.Draft)
            throw new InvalidOperationException($"Cannot confirm receiving in '{receiving.Status}' status. Must be 'Draft'.");

        receiving.Status = ReceivingStatus.Confirmed;

        // Auto-create PutAwayTasks for each OK-condition detail
        foreach (var detail in receiving.ReceivingDetails)
        {
            if (detail.Condition != ProductCondition.Ok)
                continue;

            var putAway = new PutAwayTask
            {
                ReceivingDetailId = detail.Id,
                ProductId = detail.ProductId,
                Quantity = detail.ActualQuantity,
                Status = PutAwayTaskStatus.Open
            };
            await _putAwayRepo.AddAsync(putAway);
        }

        await _repo.UpdateAsync(receiving);
        return _mapper.Map<ReceivingDto>(receiving);
    }

    public async Task<ReceivingDto?> UpdateAsync(Guid id, CreateReceivingDto dto, Guid userId)
    {
        var receiving = await _repo.GetByIdAsync(id);
        if (receiving == null) return null;

        if (receiving.Status != ReceivingStatus.Draft)
            throw new InvalidOperationException($"Cannot update receiving in '{receiving.Status}' status. Must be 'Draft'.");

        var po = await _poRepo.GetByIdAsync(dto.PurchaseOrderId);
        if (po == null)
            throw new InvalidOperationException("PurchaseOrder not found.");

        var validProductIds = po.PurchaseOrderDetails
            .Select(d => d.ProductId)
            .ToList();

        foreach (var detail in dto.Details)
        {
            if (!validProductIds.Contains(detail.ProductId))
                throw new InvalidOperationException(
                    $"Product '{detail.ProductId}' is not in PurchaseOrder '{po.PoNumber}'.");
        }

        receiving.PurchaseOrderId = dto.PurchaseOrderId;
        receiving.Notes = dto.Notes;

        receiving.ReceivingDetails.Clear();
        receiving.ReceivingDetails = dto.Details.Select(d => new ReceivingDetail
        {
            ReceivingId = receiving.Id,
            ProductId = d.ProductId,
            ExpectedQuantity = d.ExpectedQuantity,
            ActualQuantity = d.ActualQuantity,
            Condition = d.Condition
        }).ToList();

        await _repo.UpdateAsync(receiving);
        return _mapper.Map<ReceivingDto>(receiving);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var receiving = await _repo.GetByIdAsync(id);
        if (receiving == null) return false;

        if (receiving.Status != ReceivingStatus.Draft)
            throw new InvalidOperationException($"Cannot delete receiving in '{receiving.Status}' status. Must be 'Draft'.");

        await _repo.DeleteAsync(receiving);
        return true;
    }
}
