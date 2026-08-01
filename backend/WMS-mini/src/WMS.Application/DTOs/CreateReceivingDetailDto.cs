using System.ComponentModel.DataAnnotations;
using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class CreateReceivingDetailDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int ExpectedQuantity { get; set; }

    [Range(1, int.MaxValue)]
    public int ActualQuantity { get; set; }

    public ProductCondition Condition { get; set; } = ProductCondition.Ok;
}
