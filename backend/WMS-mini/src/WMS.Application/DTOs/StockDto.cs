namespace WMS.Application.DTOs;

public class StockDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int OnhandQty { get; set; }
    public int ReservedQty { get; set; }
    public int AvailableQty => OnhandQty - ReservedQty;
}