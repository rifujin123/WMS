namespace WMS.Application.DTOs;

public class CreateProductDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string? Unit { get; set; }
    public decimal Price { get; set; }
    public string? Dimension { get; set; }
}
