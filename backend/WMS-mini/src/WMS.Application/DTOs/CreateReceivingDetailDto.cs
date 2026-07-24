using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class CreateReceivingDetailDto
{
    public Guid ProductId { get; set; }
    public int ActualQuantity { get; set; }
    public ProductCondition Condition { get; set; }
}
