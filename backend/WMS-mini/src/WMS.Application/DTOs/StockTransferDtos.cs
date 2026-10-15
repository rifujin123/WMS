using System.ComponentModel.DataAnnotations;
using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class StockTransferDetailDto
{
    public Guid Id { get; set; }
    public Guid StockTransferId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid FromLocationId { get; set; }
    public string FromLocationCode { get; set; } = string.Empty;
    public Guid? ToLocationId { get; set; }
    public string? ToLocationCode { get; set; }
    public int TransferQty { get; set; }
    public int ReceivedQty { get; set; }
}

public class StockTransferDto
{
    public Guid Id { get; set; }
    public string TransferNo { get; set; } = string.Empty;
    public Guid FromWarehouseId { get; set; }
    public string FromWarehouseName { get; set; } = string.Empty;
    public Guid ToWarehouseId { get; set; }
    public string ToWarehouseName { get; set; } = string.Empty;
    public StockTransferStatus Status { get; set; }
    public DateTime? ShippedDateUtc { get; set; }
    public DateTime? ReceivedDateUtc { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<StockTransferDetailDto> Details { get; set; } = new();
}

public class CreateStockTransferDetailDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid FromLocationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng chuyển phải lớn hơn 0.")]
    public int TransferQty { get; set; }
}

public class CreateStockTransferDto
{
    [Required]
    public Guid FromWarehouseId { get; set; }

    [Required]
    public Guid ToWarehouseId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Phiếu điều chuyển phải có ít nhất một sản phẩm.")]
    public List<CreateStockTransferDetailDto> Details { get; set; } = new();
}

public class ReceiveStockTransferItemDto
{
    [Required]
    public Guid DetailId { get; set; }

    [Required]
    public Guid ToLocationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhận phải lớn hơn 0.")]
    public int ReceivedQty { get; set; }
}

public class ReceiveStockTransferDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Phải có ít nhất một dòng hàng được nhận.")]
    public List<ReceiveStockTransferItemDto> Items { get; set; } = new();
}
