namespace WMS.Application.DTOs;

public class StockSummaryDto
{
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int TotalOnhand { get; set; }
    public int TotalReserved { get; set; }
    public int LocationCount { get; set; }
}
