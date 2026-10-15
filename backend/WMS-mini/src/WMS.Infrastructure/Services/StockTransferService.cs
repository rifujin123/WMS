using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class StockTransferService : IStockTransferService
{
    private readonly WmsDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<StockTransferService> _logger;

    public StockTransferService(
        WmsDbContext db,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ILogger<StockTransferService> logger)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<List<StockTransferDto>> GetAllAsync()
    {
        var transfers = await _db.StockTransfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Details)
                .ThenInclude(d => d.Product)
            .Include(t => t.Details)
                .ThenInclude(d => d.FromLocation)
            .Include(t => t.Details)
                .ThenInclude(d => d.ToLocation)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

        return transfers.Select(MapToDto).ToList();
    }

    public async Task<StockTransferDto?> GetByIdAsync(Guid id)
    {
        var transfer = await _db.StockTransfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Details)
                .ThenInclude(d => d.Product)
            .Include(t => t.Details)
                .ThenInclude(d => d.FromLocation)
            .Include(t => t.Details)
                .ThenInclude(d => d.ToLocation)
            .FirstOrDefaultAsync(t => t.Id == id);

        return transfer == null ? null : MapToDto(transfer);
    }

    public async Task<StockTransferDto> CreateAsync(CreateStockTransferDto dto)
    {
        if (dto.FromWarehouseId == dto.ToWarehouseId)
        {
            throw new InvalidOperationException("Kho gửi và kho nhận không được trùng nhau.");
        }

        var fromWh = await _db.Warehouses.FindAsync(dto.FromWarehouseId);
        if (fromWh == null) throw new InvalidOperationException("Kho gửi không tồn tại.");

        var toWh = await _db.Warehouses.FindAsync(dto.ToWarehouseId);
        if (toWh == null) throw new InvalidOperationException("Kho nhận không tồn tại.");

        var transferNo = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30].ToUpperInvariant();

        var transfer = new StockTransfer
        {
            Id = Guid.NewGuid(),
            TransferNo = transferNo,
            FromWarehouseId = dto.FromWarehouseId,
            ToWarehouseId = dto.ToWarehouseId,
            Status = StockTransferStatus.Draft,
            Notes = dto.Notes,
            CreatedDate = DateTime.UtcNow
        };

        foreach (var item in dto.Details)
        {
            if (item.TransferQty <= 0) throw new InvalidOperationException("Số lượng điều chuyển phải lớn hơn 0.");
            var loc = await _db.Locations.FindAsync(item.FromLocationId);
            if (loc == null || loc.WarehouseId != dto.FromWarehouseId)
            {
                throw new InvalidOperationException("Vị trí lấy hàng không thuộc kho gửi.");
            }

            transfer.Details.Add(new StockTransferDetail
            {
                Id = Guid.NewGuid(),
                StockTransferId = transfer.Id,
                ProductId = item.ProductId,
                FromLocationId = item.FromLocationId,
                TransferQty = item.TransferQty,
                ReceivedQty = 0
            });
        }

        _db.StockTransfers.Add(transfer);
        await _unitOfWork.SaveChangesAsync();

        return (await GetByIdAsync(transfer.Id))!;
    }

    public async Task<StockTransferDto> ShipAsync(Guid id)
    {
        var transfer = await _db.StockTransfers
            .Include(t => t.Details)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transfer == null) throw new InvalidOperationException("Không tìm thấy phiếu điều chuyển.");
        if (transfer.Status != StockTransferStatus.Draft)
        {
            throw new InvalidOperationException("Chỉ có thể xuất chuyển đi phiếu ở trạng thái Nháp (Draft).");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var detail in transfer.Details)
            {
                var stock = await _db.Stocks.FirstOrDefaultAsync(s =>
                    s.ProductId == detail.ProductId && s.LocationId == detail.FromLocationId);

                var available = stock != null ? stock.OnhandQty - stock.ReservedQty : 0;
                if (available < detail.TransferQty)
                {
                    throw new InvalidOperationException($"Số lượng khả dụng tại vị trí xuất không đủ ({available} < {detail.TransferQty}).");
                }

                stock!.OnhandQty -= detail.TransferQty;

                var loc = await _db.Locations.FindAsync(detail.FromLocationId);
                if (loc != null)
                {
                    loc.CurrentQuantity = Math.Max(0, loc.CurrentQuantity - detail.TransferQty);
                }

                _db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = detail.ProductId,
                    LocationId = detail.FromLocationId,
                    Qty = -detail.TransferQty,
                    MovementType = MovementType.TransferOut,
                    Notes = $"Xuất điều chuyển đi theo phiếu {transfer.TransferNo}"
                });
            }

            transfer.Status = StockTransferStatus.InTransit;
            transfer.ShippedDateUtc = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        });

        _logger.LogInformation("Phiếu điều chuyển {TransferNo} đã xuất kho thành công, trạng thái InTransit.", transfer.TransferNo);
        return (await GetByIdAsync(transfer.Id))!;
    }

    public async Task<StockTransferDto> ReceiveAsync(Guid id, ReceiveStockTransferDto dto)
    {
        var transfer = await _db.StockTransfers
            .Include(t => t.Details)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transfer == null) throw new InvalidOperationException("Không tìm thấy phiếu điều chuyển.");
        if (transfer.Status != StockTransferStatus.InTransit)
        {
            throw new InvalidOperationException("Chỉ có thể nhận hàng các phiếu đang trên đường vận chuyển (InTransit).");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var item in dto.Items)
            {
                if (item.ReceivedQty <= 0) throw new InvalidOperationException("Số lượng nhận phải lớn hơn 0.");
                var detail = transfer.Details.FirstOrDefault(d => d.Id == item.DetailId);
                if (detail == null) throw new InvalidOperationException("Chi tiết phiếu điều chuyển không khớp.");

                var toLoc = await _db.Locations.FindAsync(item.ToLocationId);
                if (toLoc == null || toLoc.WarehouseId != transfer.ToWarehouseId)
                {
                    throw new InvalidOperationException("Vị trí nhập kệ không thuộc kho nhận.");
                }

                detail.ToLocationId = item.ToLocationId;
                detail.ReceivedQty = item.ReceivedQty;

                var destStock = await _db.Stocks.FirstOrDefaultAsync(s =>
                    s.ProductId == detail.ProductId && s.LocationId == item.ToLocationId);

                if (destStock == null)
                {
                    destStock = new Stock
                    {
                        Id = Guid.NewGuid(),
                        ProductId = detail.ProductId,
                        LocationId = item.ToLocationId,
                        OnhandQty = item.ReceivedQty,
                        ReservedQty = 0
                    };
                    _db.Stocks.Add(destStock);
                }
                else
                {
                    destStock.OnhandQty += item.ReceivedQty;
                }

                toLoc.CurrentQuantity += item.ReceivedQty;

                _db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = detail.ProductId,
                    LocationId = item.ToLocationId,
                    Qty = item.ReceivedQty,
                    MovementType = MovementType.TransferIn,
                    Notes = $"Nhập điều chuyển về theo phiếu {transfer.TransferNo}"
                });
            }

            transfer.Status = StockTransferStatus.Completed;
            transfer.ReceivedDateUtc = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        });

        _logger.LogInformation("Phiếu điều chuyển {TransferNo} đã nhận hàng hoàn tất, trạng thái Completed.", transfer.TransferNo);
        return (await GetByIdAsync(transfer.Id))!;
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var transfer = await _db.StockTransfers.FindAsync(id);
        if (transfer == null) return false;
        if (transfer.Status != StockTransferStatus.Draft)
        {
            throw new InvalidOperationException("Chỉ có thể hủy phiếu ở trạng thái Nháp.");
        }

        transfer.Status = StockTransferStatus.Cancelled;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static StockTransferDto MapToDto(StockTransfer entity)
    {
        return new StockTransferDto
        {
            Id = entity.Id,
            TransferNo = entity.TransferNo,
            FromWarehouseId = entity.FromWarehouseId,
            FromWarehouseName = entity.FromWarehouse?.Name ?? string.Empty,
            ToWarehouseId = entity.ToWarehouseId,
            ToWarehouseName = entity.ToWarehouse?.Name ?? string.Empty,
            Status = entity.Status,
            ShippedDateUtc = entity.ShippedDateUtc,
            ReceivedDateUtc = entity.ReceivedDateUtc,
            Notes = entity.Notes,
            CreatedDate = entity.CreatedDate,
            Details = entity.Details.Select(d => new StockTransferDetailDto
            {
                Id = d.Id,
                StockTransferId = d.StockTransferId,
                ProductId = d.ProductId,
                ProductSku = d.Product?.Sku ?? string.Empty,
                ProductName = d.Product?.Name ?? string.Empty,
                FromLocationId = d.FromLocationId,
                FromLocationCode = d.FromLocation?.Code ?? string.Empty,
                ToLocationId = d.ToLocationId,
                ToLocationCode = d.ToLocation?.Code,
                TransferQty = d.TransferQty,
                ReceivedQty = d.ReceivedQty
            }).ToList()
        };
    }
}
