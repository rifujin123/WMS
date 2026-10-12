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

    public Task<PagedResult<ReceivingDto>> GetPagedAsync(
        ReceivingListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _repo.GetPagedAsync(query, pageSize, cancellationToken);
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
        if (dto.InvoiceTotalAmount.HasValue && dto.InvoiceTotalAmount.Value < 0)
            throw new InvalidOperationException("Tổng tiền hóa đơn không được nhỏ hơn 0.");
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
            InvoiceNumber = dto.InvoiceNumber,
            InvoiceDate = dto.InvoiceDate,
            InvoiceTotalAmount = dto.InvoiceTotalAmount,
            ReceivingDetails = dto.Details.Select(d => new ReceivingDetail
            {
                ProductId = d.ProductId,
                ExpectedQuantity = d.ExpectedQuantity,
                ActualQuantity = d.ActualQuantity,
                Condition = d.Condition,
                LotNumber = d.LotNumber,
                ExpiryDate = d.ExpiryDate,
                UnitPrice = d.UnitPrice
            }).ToList()
        };
        await _repo.AddAsync(receiving);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<ReceivingDto>(receiving);
    }

    public async Task<ReceivingDto?> ConfirmAsync(Guid id)
    {
        var receiving = await _repo.GetByIdAsync(id);
        if (receiving == null) return null;
        if (receiving.Status != ReceivingStatus.Draft) throw new InvalidOperationException($"Không thể xác nhận phiếu nhận ở trạng thái '{receiving.Status}'. Chỉ có thể xác nhận phiếu 'Nháp' (Draft).");
        var po = await _poRepo.GetByIdAsync(receiving.PurchaseOrderId) ?? throw new InvalidOperationException("Không tìm thấy đơn đặt hàng.");
        ValidatePurchaseOrder(po);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ApplyReceivedQuantities(receiving, po);

            var isFullyReceived = po.PurchaseOrderDetails.All(d => d.ReceivedQuantity >= d.OrderedQuantity);
            po.Status = isFullyReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
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
        if (entity.Status != ReceivingStatus.Draft) throw new InvalidOperationException($"Không thể cập nhật phiếu nhận ở trạng thái '{entity.Status}'. Chỉ có thể cập nhật phiếu 'Nháp' (Draft).");
        var po = await GetPurchaseOrderAsync(dto);
        ValidatePurchaseOrder(po);
        if (dto.InvoiceTotalAmount.HasValue && dto.InvoiceTotalAmount.Value < 0)
            throw new InvalidOperationException("Tổng tiền hóa đơn không được nhỏ hơn 0.");
        ValidateDetails(dto, po);
        ValidateInvoiceImageUrl(dto.InvoiceImageUrl);
        entity.PurchaseOrderId = dto.PurchaseOrderId;
        entity.Notes = dto.Notes;
        entity.InvoiceImageUrl = dto.InvoiceImageUrl;
        entity.InvoiceNumber = dto.InvoiceNumber;
        entity.InvoiceDate = dto.InvoiceDate;
        entity.InvoiceTotalAmount = dto.InvoiceTotalAmount;
        await _repo.RemoveDetailsAsync(entity.Id);
        entity.ReceivingDetails = dto.Details.Select(d => new ReceivingDetail
        {
            ReceivingId = entity.Id,
            ProductId = d.ProductId,
            ExpectedQuantity = d.ExpectedQuantity,
            ActualQuantity = d.ActualQuantity,
            Condition = d.Condition,
            LotNumber = d.LotNumber,
            ExpiryDate = d.ExpiryDate,
            UnitPrice = d.UnitPrice
        }).ToList();
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<ReceivingDto>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;
        if (entity.Status != ReceivingStatus.Draft) throw new InvalidOperationException($"Không thể xóa phiếu nhận ở trạng thái '{entity.Status}'. Chỉ có thể xóa phiếu 'Nháp' (Draft).");
        await _repo.DeleteAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<PurchaseOrder> GetPurchaseOrderAsync(CreateReceivingDto dto) => await _poRepo.GetByIdAsync(dto.PurchaseOrderId) ?? throw new InvalidOperationException("Không tìm thấy đơn đặt hàng.");

    private static void ValidatePurchaseOrder(PurchaseOrder po)
    {
        if (po.Status != PurchaseOrderStatus.Approved && po.Status != PurchaseOrderStatus.PartiallyReceived)
            throw new InvalidOperationException("Đơn đặt hàng phải ở trạng thái Đã duyệt (Approved) hoặc Đã nhận một phần (PartiallyReceived) trước khi nhận hàng.");
    }

    private static void ValidateInvoiceImageUrl(string? invoiceImageUrl)
    {
        if (string.IsNullOrWhiteSpace(invoiceImageUrl)) return;
        if (!Uri.TryCreate(invoiceImageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Đường dẫn ảnh hóa đơn phải là URL HTTP hoặc HTTPS hợp lệ.");
    }

    private static void ApplyReceivedQuantities(Receiving receiving, PurchaseOrder po)
    {
        var poDetailsByProduct = po.PurchaseOrderDetails
            .GroupBy(d => d.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var detailGroup in receiving.ReceivingDetails.GroupBy(d => d.ProductId))
        {
            if (!poDetailsByProduct.TryGetValue(detailGroup.Key, out var poDetails))
                throw new InvalidOperationException($"Không thể xác nhận: Sản phẩm không thuộc đơn đặt hàng '{po.PoNumber}'.");

            var quantity = detailGroup.Sum(d => d.ActualQuantity);
            var totalRemaining = poDetails.Sum(d => Math.Max(0, d.OrderedQuantity - d.ReceivedQuantity));
            if (quantity > totalRemaining)
                throw new InvalidOperationException($"Không thể xác nhận: Số lượng nhận {quantity} vượt quá số lượng đặt còn lại của sản phẩm.");

            var remaining = quantity;
            foreach (var poDetail in poDetails)
            {
                var canApply = poDetail.OrderedQuantity - poDetail.ReceivedQuantity;
                if (canApply > 0)
                {
                    var appliedQuantity = Math.Min(remaining, canApply);
                    poDetail.ReceivedQuantity += appliedQuantity;
                    remaining -= appliedQuantity;
                    if (remaining == 0) break;
                }
            }
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
                throw new InvalidOperationException($"Số lượng nhận của sản phẩm phải lớn hơn 0.");

            if (detail.UnitPrice.HasValue && detail.UnitPrice.Value < 0)
                throw new InvalidOperationException("Đơn giá không được nhỏ hơn 0.");

            if (!poDetails.TryGetValue(detail.ProductId, out var poDetail))
                throw new InvalidOperationException($"Sản phẩm không thuộc đơn đặt hàng '{po.PoNumber}'.");
        }

        foreach (var detailGroup in dto.Details.GroupBy(d => d.ProductId))
        {
            var poDetail = poDetails[detailGroup.Key];
            var quantity = detailGroup.Sum(d => d.ActualQuantity);
            var remaining = poDetail.OrderedQuantity - poDetail.ReceivedQuantity;
            if (quantity > remaining)
                throw new InvalidOperationException($"Không thể nhận {quantity} sản phẩm vượt quá số lượng còn lại. Số lượng còn lại cần nhận là {remaining}.");
        }
    }
}
