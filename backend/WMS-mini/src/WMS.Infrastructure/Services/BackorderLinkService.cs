using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services;

public class BackorderLinkService : IBackorderLinkService
{
    private readonly IBackorderLinkRepository _linkRepo;
    private readonly ISaleOrderRepository _saleOrderRepo;
    private readonly IPurchaseOrderRepository _poRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BackorderLinkService(
        IBackorderLinkRepository linkRepo,
        ISaleOrderRepository saleOrderRepo,
        IPurchaseOrderRepository poRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _linkRepo = linkRepo;
        _saleOrderRepo = saleOrderRepo;
        _poRepo = poRepo;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<BackorderLinkDto>> GetAllAsync(Guid? saleOrderId = null, Guid? purchaseOrderId = null, Guid? productId = null)
    {
        var links = await _linkRepo.GetAllAsync(saleOrderId, purchaseOrderId, productId);
        return _mapper.Map<List<BackorderLinkDto>>(links);
    }

    public async Task<BackorderLinkDto?> GetByIdAsync(Guid id)
    {
        var link = await _linkRepo.GetByIdAsync(id);
        if (link == null) return null;
        return _mapper.Map<BackorderLinkDto>(link);
    }

    public async Task<BackorderLinkDto> CreateAsync(CreateBackorderLinkDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Quantity <= 0)
            throw new InvalidOperationException("Số lượng liên kết phải lớn hơn 0.");

        var soDetail = await _saleOrderRepo.GetDetailByIdAsync(dto.SaleOrderDetailId)
            ?? throw new InvalidOperationException("Không tìm thấy dòng đơn bán hàng.");

        var saleOrder = soDetail.SaleOrder
            ?? await _saleOrderRepo.GetByIdAsync(soDetail.SaleOrderId)
            ?? throw new InvalidOperationException("Không tìm thấy đơn bán hàng.");

        if (saleOrder.Status == SaleOrderStatus.AwaitingDeposit)
        {
            throw new InvalidOperationException("Chỉ được gán nguồn hàng khi khách đã xác nhận nộp tiền cọc (Backorder).");
        }

        if (saleOrder.Status != SaleOrderStatus.Backorder)
        {
            throw new InvalidOperationException($"Chỉ được gán nguồn hàng cho đơn ở trạng thái 'Chờ hàng' (Backorder). Trạng thái hiện tại: '{saleOrder.Status}'.");
        }

        var poDetail = await _poRepo.GetDetailByIdAsync(dto.PurchaseOrderDetailId)
            ?? throw new InvalidOperationException("Không tìm thấy dòng đơn mua hàng.");

        var purchaseOrder = poDetail.PurchaseOrder
            ?? await _poRepo.GetByIdAsync(poDetail.PurchaseOrderId)
            ?? throw new InvalidOperationException("Không tìm thấy đơn mua hàng.");

        if (purchaseOrder.Status != PurchaseOrderStatus.Approved && purchaseOrder.Status != PurchaseOrderStatus.PartiallyReceived)
        {
            throw new InvalidOperationException("Chỉ có thể liên kết với đơn mua hàng ở trạng thái 'Đã duyệt' hoặc 'Đang nhận hàng'.");
        }

        if (soDetail.ProductId != poDetail.ProductId)
        {
            throw new InvalidOperationException("Sản phẩm của dòng đơn bán và đơn mua không khớp nhau.");
        }

        if (saleOrder.WarehouseId != purchaseOrder.WarehouseId)
        {
            throw new InvalidOperationException("Kho hàng của đơn bán và đơn mua không khớp nhau.");
        }

        var existingLinks = await _linkRepo.GetBySaleOrderDetailIdAsync(soDetail.Id);
        var existingUnfulfilledQty = existingLinks.Where(l => !l.IsDeleted && !l.IsFulfilled).Sum(l => l.Quantity - l.FulfilledQuantity);
        var remainingNeed = soDetail.Quantity - soDetail.AllocatedQty - existingUnfulfilledQty;

        if (dto.Quantity > remainingNeed)
        {
            throw new InvalidOperationException(
                $"Số lượng liên kết ({dto.Quantity}) vượt quá số lượng cần bù còn lại ({Math.Max(0, remainingNeed)}) của dòng đơn bán.");
        }

        var existingPoLinks = await _linkRepo.GetByPurchaseOrderDetailIdAsync(poDetail.Id);
        var existingPoUnfulfilledQty = existingPoLinks.Where(l => !l.IsDeleted && !l.IsFulfilled).Sum(l => l.Quantity - l.FulfilledQuantity);
        var poRemainingCapacity = poDetail.OrderedQuantity - poDetail.ReceivedQuantity - existingPoUnfulfilledQty;

        if (dto.Quantity > poRemainingCapacity)
        {
            throw new InvalidOperationException(
                $"Số lượng liên kết ({dto.Quantity}) vượt quá số lượng khả dụng còn lại của dòng đơn mua ({Math.Max(0, poRemainingCapacity)}).");
        }

        var link = new BackorderLink
        {
            SaleOrderDetailId = dto.SaleOrderDetailId,
            PurchaseOrderDetailId = dto.PurchaseOrderDetailId,
            Quantity = dto.Quantity,
            FulfilledQuantity = 0,
            IsFulfilled = false,
            CreatedDate = DateTime.UtcNow
        };

        await _linkRepo.AddAsync(link);
        await _unitOfWork.SaveChangesAsync();

        return (await GetByIdAsync(link.Id))!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var link = await _linkRepo.GetByIdAsync(id);
        if (link == null) return false;

        if (link.FulfilledQuantity > 0)
        {
            throw new InvalidOperationException("Không thể hủy liên kết đã được nhận một phần hoặc hoàn tất.");
        }

        await _linkRepo.DeleteAsync(link);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
