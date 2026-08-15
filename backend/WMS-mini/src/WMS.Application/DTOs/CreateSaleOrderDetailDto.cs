using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class CreateSaleOrderDetailDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
