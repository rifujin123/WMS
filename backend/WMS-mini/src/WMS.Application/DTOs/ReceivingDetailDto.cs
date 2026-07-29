using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class ReceivingDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int ActualQuantity { get; set; }
    public ProductCondition Condition { get; set; }
}
