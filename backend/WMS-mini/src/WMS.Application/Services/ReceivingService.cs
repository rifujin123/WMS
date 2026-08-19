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
        ValidatePurchaseOrder(po);
        if (await _repo.GetConfirmedByPurchaseOrderIdAsync(dto.PurchaseOrderId) != null)
            throw new InvalidOperationException("PurchaseOrder already has a confirmed receiving.");
        ValidateDetails(dto, po);
        ValidateInvoiceImageUrl(dto.InvoiceImageUrl);
        var now = DateTime.UtcNow;
        var receiving = new Receiving
        {
            ReceivingNo = $"RC-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..8]}",
            PurchaseOrderId = dto.PurchaseOrderId, 
            ReceivedById = _currentUser.UserId, 
            ReceivedDate = now,
            Status = ReceivingStatus.Draft, 
            Notes = dto.Notes,
            InvoiceImageUrl = dto.InvoiceImageUrl,
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
        ValidatePurchaseOrder(po);
        if (await _repo.GetConfirmedByPurchaseOrderIdAsync(receiving.PurchaseOrderId) != null)
            throw new InvalidOperationException("PurchaseOrder already has a confirmed receiving.");
        ValidateCompleteReceiving(receiving, po);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ApplyReceivedQuantities(receiving, po);

            po.Status = PurchaseOrderStatus.Received;
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
        if (await _repo.GetConfirmedByPurchaseOrderIdAsync(dto.PurchaseOrderId) is { Id: var confirmedId } && confirmedId != entity.Id)
            throw new InvalidOperationException("PurchaseOrder already has a confirmed receiving.");
        ValidatePurchaseOrder(po);
        ValidateDetails(dto, po);
        ValidateInvoiceImageUrl(dto.InvoiceImageUrl);
        entity.PurchaseOrderId = dto.PurchaseOrderId; entity.Notes = dto.Notes; entity.InvoiceImageUrl = dto.InvoiceImageUrl;
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

    private static void ValidatePurchaseOrder(PurchaseOrder po)
    {
        if (po.Status != PurchaseOrderStatus.Approved)
            throw new InvalidOperationException("PurchaseOrder must be Approved before receiving.");
    }

    private static void ValidateInvoiceImageUrl(string? invoiceImageUrl)
    {
        if (string.IsNullOrWhiteSpace(invoiceImageUrl)) return;
        if (!Uri.TryCreate(invoiceImageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("InvoiceImageUrl must be an absolute HTTP or HTTPS URL.");
    }

    private static void ValidateCompleteReceiving(Receiving receiving, PurchaseOrder po)
    {
        var receivingByProduct = receiving.ReceivingDetails
            .GroupBy(d => d.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.ActualQuantity));

        foreach (var poDetail in po.PurchaseOrderDetails.GroupBy(d => d.ProductId).Select(g => new
        {
            ProductId = g.Key,
            OrderedQuantity = g.Sum(d => d.OrderedQuantity),
            ReceivedQuantity = g.Sum(d => d.ReceivedQuantity)
        }))
        {
            var receivingQuantity = receivingByProduct.GetValueOrDefault(poDetail.ProductId);
            if (poDetail.ReceivedQuantity + receivingQuantity != poDetail.OrderedQuantity)
                throw new InvalidOperationException("Cannot confirm receiving until all PurchaseOrder quantities are processed.");
        }
    }

    private static void ApplyReceivedQuantities(Receiving receiving, PurchaseOrder po)
    {
        var poDetailsByProduct = po.PurchaseOrderDetails
            .GroupBy(d => d.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var detailGroup in receiving.ReceivingDetails.GroupBy(d => d.ProductId))
        {
            var poDetails = poDetailsByProduct[detailGroup.Key];
            var quantity = detailGroup
                .Where(d => d.Condition == ProductCondition.Ok)
                .Sum(d => d.ActualQuantity);
            var remaining = quantity;

            foreach (var poDetail in poDetails)
            {
                var appliedQuantity = Math.Min(remaining, poDetail.OrderedQuantity - poDetail.ReceivedQuantity);
                poDetail.ReceivedQuantity += appliedQuantity;
                remaining -= appliedQuantity;
                if (remaining == 0) break;
            }

            if (remaining != 0)
                throw new InvalidOperationException($"Cannot confirm: receiving {quantity} would exceed the ordered quantity for product '{detailGroup.Key}'.");
        }
    }

    private static void ValidateDetails(CreateReceivingDto dto, PurchaseOrder po)
    {
        var poDetails = po.PurchaseOrderDetails
            .GroupBy(d => d.ProductId)
            .ToDictionary(g => g.Key, g => new
            {
                OrderedQuantity = g.Sum(d => d.OrderedQuantity),
                ReceivedQuantity = g.Sum(d => d.ReceivedQuantity)
            });

        foreach (var detail in dto.Details)
        {
            if (detail.ActualQuantity <= 0)
                throw new InvalidOperationException($"Quantity for product '{detail.ProductId}' must be greater than zero.");

            if (!poDetails.TryGetValue(detail.ProductId, out var poDetail))
                throw new InvalidOperationException($"Product '{detail.ProductId}' is not in PurchaseOrder '{po.PoNumber}'.");
        }

        foreach (var detailGroup in dto.Details.GroupBy(d => d.ProductId))
        {
            var poDetail = poDetails[detailGroup.Key];
            var quantity = detailGroup.Sum(d => d.ActualQuantity);
            var remaining = poDetail.OrderedQuantity - poDetail.ReceivedQuantity;
            if (quantity > remaining)
                throw new InvalidOperationException($"Cannot receive {quantity} of product '{detailGroup.Key}'. Remaining: {remaining}.");
        }
    }
}
