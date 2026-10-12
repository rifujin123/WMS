using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class CreateSaleOrderDto
{
    [Required]
    [MaxLength(50)]
    public string OrderNo { get; set; } = string.Empty;

    public Guid? CustomerId { get; set; }

    [MaxLength(200)]
    public string? CustomerName { get; set; }

    public DateTime OrderDate { get; set; }

    public Guid? WarehouseId { get; set; }

    public bool IsPreOrder { get; set; }
    public decimal RequiredDeposit { get; set; }
    public decimal PaidDeposit { get; set; }
    public DateTime? DepositDeadline { get; set; }

    [MaxLength(500)]
    public string? DepositNotes { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateSaleOrderDetailDto> SaleOrderDetails { get; set; } = new();
}
