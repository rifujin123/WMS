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
    private readonly ILocationRepository? _locationRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public ReceivingService(
        IReceivingRepository repo,
        IPutAwayTaskRepository putAwayRepo,
        IPurchaseOrderRepository poRepo,
        ILocationRepository locationRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _repo = repo;
        _putAwayRepo = putAwayRepo;
        _poRepo = poRepo;
        _locationRepo = locationRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public ReceivingService(
        IReceivingRepository repo,
        IPutAwayTaskRepository putAwayRepo,
        IPurchaseOrderRepository poRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IMapper mapper)
        : this(repo, putAwayRepo, poRepo, null!, unitOfWork, currentUser, mapper)
    {
    }

    public async Task<List<ReceivingDto>> GetAllAsync() => _mapper.Map<List<ReceivingDto>>(await _repo.GetAllAsync());

    public Task<PagedResult<ReceivingDto>> GetPagedAsync(
        ReceivingListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _repo.GetPagedAsync(query, pageSize, cancellationToken);

    public async Task<ReceivingDto?> GetByIdAsync(Guid id)
    {
        var receiving = await _repo.GetByIdAsync(id);
        if (receiving == null)
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

        var (hasDiscrepancy, reason, summary) = EvaluateDiscrepancies(dto.Details, po, dto.InvoiceTotalAmount);

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
            HasDiscrepancy = hasDiscrepancy,
            DiscrepancyReason = reason,
            ReceivingDetails = dto.Details.Select(d => new ReceivingDetail
            {
                ProductId = d.ProductId,
                ExpectedQuantity = d.ExpectedQuantity,
                ActualQuantity = d.ActualQuantity,
                Condition = d.Condition,
                LotNumber = d.LotNumber,
                ExpiryDate = d.ExpiryDate,
                UnitPrice = d.UnitPrice,
                DiscrepancyAction = d.DiscrepancyAction ?? (d.Condition == ProductCondition.Damaged ? DiscrepancyAction.QuarantineAndHold : null),
                DiscrepancyNotes = d.DiscrepancyNotes
            }).ToList()
        };

        if (hasDiscrepancy)
        {
            receiving.DiscrepancyReport = new DiscrepancyReport
            {
                ReceivingId = receiving.Id,
                PurchaseOrderId = po.Id,
                VendorName = po.VendorName,
                Status = DiscrepancyReportStatus.Pending,
                Notes = reason,
                ResolutionSummary = summary
            };
        }

        await _repo.AddAsync(receiving);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<ReceivingDto>(receiving);
    }

    public async Task<ReceivingDto?> ConfirmAsync(Guid id)
    {
        var receiving = await _repo.GetByIdAsync(id);
        if (receiving == null) return null;
        if (receiving.Status != ReceivingStatus.Draft)
            throw new InvalidOperationException($"Không thể xác nhận phiếu nhận ở trạng thái '{receiving.Status}'. Chỉ có thể xác nhận phiếu 'Nháp' (Draft).");

        var po = await _poRepo.GetByIdAsync(receiving.PurchaseOrderId) ?? throw new InvalidOperationException("Không tìm thấy đơn đặt hàng.");
        ValidatePurchaseOrder(po);

        // Check if any line has damaged items requiring Quarantine
        var hasQuarantineItems = receiving.ReceivingDetails.Any(d =>
            d.Condition == ProductCondition.Damaged &&
            d.ActualQuantity > 0 &&
            (d.DiscrepancyAction == DiscrepancyAction.QuarantineAndHold || d.DiscrepancyAction == null));

        Location? quarantineLocation = null;
        if (hasQuarantineItems)
        {
            if (_locationRepo != null)
            {
                if (po.WarehouseId.HasValue)
                {
                    var warehouseLocations = await _locationRepo.GetByWarehouseIdAsync(po.WarehouseId.Value);
                    quarantineLocation = warehouseLocations.FirstOrDefault(l => l.LocationType == LocationType.Quarantine);
                }
                else
                {
                    var allLocations = await _locationRepo.GetAllAsync();
                    quarantineLocation = allLocations.FirstOrDefault(l => l.LocationType == LocationType.Quarantine);
                }
            }

            if (quarantineLocation == null)
            {
                throw new InvalidOperationException("Kho chưa cấu hình vị trí cách ly (Quarantine). Vui lòng cấu hình vị trí cách ly trước.");
            }
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ApplyReceivedQuantities(receiving, po);

            var isFullyReceived = po.PurchaseOrderDetails.All(d => d.ReceivedQuantity >= d.OrderedQuantity);
            po.Status = isFullyReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
            receiving.Status = ReceivingStatus.Confirmed;
            receiving.ConfirmedById = _currentUser.UserId;
            receiving.ConfirmedDate = DateTime.UtcNow;

            // 1. Ok goods -> create standard PutAwayTask
            foreach (var detail in receiving.ReceivingDetails.Where(d => d.Condition == ProductCondition.Ok && d.ActualQuantity > 0))
            {
                await _putAwayRepo.AddAsync(new PutAwayTask
                {
                    ReceivingDetailId = detail.Id,
                    ProductId = detail.ProductId,
                    Quantity = detail.ActualQuantity,
                    Status = PutAwayTaskStatus.Open
                });
            }

            // 2. Damaged goods with QuarantineAndHold -> create PutAwayTask targeting Quarantine location
            foreach (var detail in receiving.ReceivingDetails.Where(d => d.Condition == ProductCondition.Damaged && d.ActualQuantity > 0))
            {
                if (detail.DiscrepancyAction == DiscrepancyAction.QuarantineAndHold || detail.DiscrepancyAction == null)
                {
                    detail.DiscrepancyAction = DiscrepancyAction.QuarantineAndHold;
                    await _putAwayRepo.AddAsync(new PutAwayTask
                    {
                        ReceivingDetailId = detail.Id,
                        ProductId = detail.ProductId,
                        Quantity = detail.ActualQuantity,
                        ToLocationId = quarantineLocation!.Id,
                        Status = PutAwayTaskStatus.Open
                    });
                }
                // ReturnToVendor: no PutAwayTask created
            }

            // Check discrepancy
            var detailDtos = receiving.ReceivingDetails.Select(d => new CreateReceivingDetailDto
            {
                ProductId = d.ProductId,
                ExpectedQuantity = d.ExpectedQuantity,
                ActualQuantity = d.ActualQuantity,
                Condition = d.Condition,
                UnitPrice = d.UnitPrice,
                LotNumber = d.LotNumber,
                ExpiryDate = d.ExpiryDate,
                DiscrepancyAction = d.DiscrepancyAction,
                DiscrepancyNotes = d.DiscrepancyNotes
            });

            var (hasDiscrepancy, reason, summary) = EvaluateDiscrepancies(detailDtos, po, receiving.InvoiceTotalAmount);
            receiving.HasDiscrepancy = hasDiscrepancy;
            receiving.DiscrepancyReason = reason;

            if (hasDiscrepancy)
            {
                var existingReport = await _repo.GetDiscrepancyReportByReceivingIdAsync(receiving.Id);
                if (existingReport == null)
                {
                    var report = new DiscrepancyReport
                    {
                        ReceivingId = receiving.Id,
                        PurchaseOrderId = po.Id,
                        VendorName = po.VendorName,
                        Status = DiscrepancyReportStatus.Pending,
                        Notes = reason,
                        ResolutionSummary = summary
                    };
                    await _repo.AddDiscrepancyReportAsync(report);
                    receiving.DiscrepancyReport = report;
                }
            }

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
        if (entity.Status != ReceivingStatus.Draft)
            throw new InvalidOperationException($"Không thể cập nhật phiếu nhận ở trạng thái '{entity.Status}'. Chỉ có thể cập nhật phiếu 'Nháp' (Draft).");

        var po = await GetPurchaseOrderAsync(dto);
        ValidatePurchaseOrder(po);
        if (dto.InvoiceTotalAmount.HasValue && dto.InvoiceTotalAmount.Value < 0)
            throw new InvalidOperationException("Tổng tiền hóa đơn không được nhỏ hơn 0.");
        ValidateDetails(dto, po);
        ValidateInvoiceImageUrl(dto.InvoiceImageUrl);

        var (hasDiscrepancy, reason, summary) = EvaluateDiscrepancies(dto.Details, po, dto.InvoiceTotalAmount);

        entity.PurchaseOrderId = dto.PurchaseOrderId;
        entity.Notes = dto.Notes;
        entity.InvoiceImageUrl = dto.InvoiceImageUrl;
        entity.InvoiceNumber = dto.InvoiceNumber;
        entity.InvoiceDate = dto.InvoiceDate;
        entity.InvoiceTotalAmount = dto.InvoiceTotalAmount;
        entity.HasDiscrepancy = hasDiscrepancy;
        entity.DiscrepancyReason = reason;

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
            UnitPrice = d.UnitPrice,
            DiscrepancyAction = d.DiscrepancyAction ?? (d.Condition == ProductCondition.Damaged ? DiscrepancyAction.QuarantineAndHold : null),
            DiscrepancyNotes = d.DiscrepancyNotes
        }).ToList();

        if (hasDiscrepancy)
        {
            var existingReport = await _repo.GetDiscrepancyReportByReceivingIdAsync(entity.Id);
            if (existingReport == null)
            {
                var report = new DiscrepancyReport
                {
                    ReceivingId = entity.Id,
                    PurchaseOrderId = po.Id,
                    VendorName = po.VendorName,
                    Status = DiscrepancyReportStatus.Pending,
                    Notes = reason,
                    ResolutionSummary = summary
                };
                await _repo.AddDiscrepancyReportAsync(report);
                entity.DiscrepancyReport = report;
            }
            else
            {
                existingReport.Notes = reason;
                if (!string.IsNullOrWhiteSpace(summary))
                    existingReport.ResolutionSummary = summary;
                await _repo.UpdateDiscrepancyReportAsync(existingReport);
            }
        }
        else
        {
            var existingReport = await _repo.GetDiscrepancyReportByReceivingIdAsync(entity.Id);
            if (existingReport != null)
            {
                await _repo.DeleteDiscrepancyReportAsync(existingReport);
            }
        }

        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<ReceivingDto>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;
        if (entity.Status != ReceivingStatus.Draft)
            throw new InvalidOperationException($"Không thể xóa phiếu nhận ở trạng thái '{entity.Status}'. Chỉ có thể xóa phiếu 'Nháp' (Draft).");
        await _repo.DeleteAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<DiscrepancyReportDto?> GetDiscrepancyReportAsync(Guid receivingId)
    {
        var report = await _repo.GetDiscrepancyReportByReceivingIdAsync(receivingId);
        if (report == null)
        {
            var receiving = await _repo.GetByIdAsync(receivingId);
            if (receiving?.DiscrepancyReport != null)
                report = receiving.DiscrepancyReport;
        }

        return report == null ? null : _mapper.Map<DiscrepancyReportDto>(report);
    }

    public async Task<DiscrepancyReportDto> ApproveDiscrepancyReportAsync(Guid receivingId, ApproveDiscrepancyReportDto dto)
    {
        if (!_currentUser.IsInRole("Admin") && !_currentUser.IsInRole("WarehouseManager"))
        {
            throw new UnauthorizedAccessException("Chỉ Quản lý kho (WarehouseManager) hoặc Admin mới có quyền duyệt biên bản bất thường.");
        }

        var report = await _repo.GetDiscrepancyReportByReceivingIdAsync(receivingId);
        if (report == null)
        {
            var receiving = await _repo.GetByIdAsync(receivingId);
            if (receiving == null)
                throw new InvalidOperationException("Không tìm thấy phiếu nhận.");
            if (!receiving.HasDiscrepancy)
                throw new InvalidOperationException("Phiếu nhận không có biên bản bất thường.");
            report = receiving.DiscrepancyReport;
        }

        if (report == null)
            throw new InvalidOperationException("Không tìm thấy biên bản bất thường cho phiếu nhận này.");

        if (report.Status != DiscrepancyReportStatus.Pending)
            throw new InvalidOperationException($"Biên bản bất thường đã được xử lý (trạng thái hiện tại: {report.Status}).");

        report.Status = dto.IsApproved ? DiscrepancyReportStatus.Approved : DiscrepancyReportStatus.Rejected;
        report.ApprovedById = _currentUser.UserId;
        report.ApprovedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            report.Notes = dto.Notes;
            report.ResolutionSummary = string.IsNullOrWhiteSpace(report.ResolutionSummary)
                ? dto.Notes
                : $"{report.ResolutionSummary} | Ghi chú duyệt: {dto.Notes}";
        }

        await _repo.UpdateDiscrepancyReportAsync(report);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<DiscrepancyReportDto>(report);
    }

    private async Task<PurchaseOrder> GetPurchaseOrderAsync(CreateReceivingDto dto) =>
        await _poRepo.GetByIdAsync(dto.PurchaseOrderId) ?? throw new InvalidOperationException("Không tìm thấy đơn đặt hàng.");

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

            // Exclude items returned to vendor immediately at the dock
            var quantity = detailGroup
                .Where(d => d.DiscrepancyAction != DiscrepancyAction.ReturnToVendor)
                .Sum(d => d.ActualQuantity);
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
            if (detail.ActualQuantity < 0)
                throw new InvalidOperationException("Số lượng nhận của sản phẩm không được nhỏ hơn 0.");

            if (detail.ActualQuantity == 0 && detail.DiscrepancyAction == null && detail.Condition != ProductCondition.Missing)
                throw new InvalidOperationException("Số lượng nhận bằng 0 phải đính kèm hành động xử lý thiếu.");

            if (detail.DiscrepancyAction.HasValue && !Enum.IsDefined(typeof(DiscrepancyAction), detail.DiscrepancyAction.Value))
                throw new InvalidOperationException("Hành động xử lý sai lệch không hợp lệ.");

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

    private static (bool HasDiscrepancy, string? Reason, string? Summary) EvaluateDiscrepancies(
        IEnumerable<CreateReceivingDetailDto> details,
        PurchaseOrder po,
        decimal? invoiceTotalAmount)
    {
        var reasons = new List<string>();
        var summaries = new List<string>();
        var detailList = details.ToList();

        var poDetailsByProduct = po.PurchaseOrderDetails
            .GroupBy(d => d.ProductId)
            .ToDictionary(g => g.Key, g => g.First());

        // 1. Damaged goods
        var damaged = detailList.Where(d => d.Condition == ProductCondition.Damaged).ToList();
        if (damaged.Count > 0)
        {
            reasons.Add($"Có {damaged.Count} sản phẩm hư hỏng");
            foreach (var d in damaged)
            {
                var action = d.DiscrepancyAction ?? DiscrepancyAction.QuarantineAndHold;
                summaries.Add($"Hàng hỏng ({d.ActualQuantity}): hướng xử lý {action}");
            }
        }

        // 2. Missing or shortage
        var missing = detailList.Where(d => d.Condition == ProductCondition.Missing || d.ActualQuantity < d.ExpectedQuantity || d.ActualQuantity == 0).ToList();
        if (missing.Count > 0)
        {
            reasons.Add($"Có {missing.Count} sản phẩm thiếu hoặc chưa nhận đủ");
            foreach (var d in missing)
            {
                if (d.DiscrepancyAction.HasValue)
                {
                    summaries.Add($"Hàng thiếu ({d.ExpectedQuantity - d.ActualQuantity}): hướng xử lý {d.DiscrepancyAction.Value}");
                }
            }
        }

        // 3. Price discrepancy
        var priceMismatch = detailList.Where(d => d.UnitPrice.HasValue && poDetailsByProduct.TryGetValue(d.ProductId, out var poDetail) && Math.Abs(d.UnitPrice.Value - poDetail.UnitPrice) > 0.001m).ToList();
        if (priceMismatch.Count > 0)
        {
            reasons.Add($"Có {priceMismatch.Count} sản phẩm lệch đơn giá so với PO");
        }

        // 4. Invoice total discrepancy
        if (invoiceTotalAmount.HasValue && po.TotalAmount > 0 && Math.Abs(invoiceTotalAmount.Value - po.TotalAmount) > 0.001m)
        {
            reasons.Add("Tổng tiền hóa đơn lệch so với PO");
        }

        // 5. Missing PO line
        var receivedProductIds = detailList.Select(d => d.ProductId).ToHashSet();
        var missingPoDetails = po.PurchaseOrderDetails.Where(p => !receivedProductIds.Contains(p.ProductId) && (p.OrderedQuantity - p.ReceivedQuantity) > 0).ToList();
        if (missingPoDetails.Count > 0)
        {
            reasons.Add($"Có {missingPoDetails.Count} mặt hàng trong PO không có trong phiếu nhận");
        }

        var hasAction = detailList.Any(d => d.DiscrepancyAction.HasValue);
        var hasDiscrepancy = reasons.Count > 0 || hasAction;
        var reasonText = hasDiscrepancy ? string.Join("; ", reasons) : null;
        var summaryText = summaries.Count > 0 ? string.Join("; ", summaries) : reasonText;

        return (hasDiscrepancy, reasonText, summaryText);
    }
}
