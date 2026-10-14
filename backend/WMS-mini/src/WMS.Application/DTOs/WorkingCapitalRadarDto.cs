namespace WMS.Application.DTOs;

public class WorkingCapitalRadarDto
{
    public decimal TotalWorkingCapital { get; set; }
    public decimal TotalFrozenCapital { get; set; }
    public decimal DeadStockCapital { get; set; }
    public int NearExpiryStockCount { get; set; }
    public decimal NearExpiryStockCapital { get; set; }

    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }

    public int NearExpiryDays { get; set; }
    public int DeadStockDays { get; set; }
    public int FrozenCapitalDays { get; set; }
    public bool EnableExpiryAlertBanner { get; set; }
    public bool HasEmergencyAlert { get; set; }
    public int EmergencyAlertCount { get; set; }

    public List<AgingBucketDto> AgingBuckets { get; set; } = new();
    public List<TopDeadStockItemDto> TopDeadStocks { get; set; } = new();
    public List<FefoAlertItemDto> FefoAlerts { get; set; } = new();
}

public class AgingBucketDto
{
    public string Range { get; set; } = string.Empty;
    public int MinDays { get; set; }
    public int? MaxDays { get; set; }
    public decimal TotalValue { get; set; }
    public int Quantity { get; set; }
    public decimal Percentage { get; set; }
}

public class TopDeadStockItemDto
{
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal TiedUpCapital { get; set; }
    public decimal BreakEvenPrice { get; set; }
    public int DaysInStock { get; set; }
    public List<string> Locations { get; set; } = new();
    public string ActionSuggestion { get; set; } = "Xả hàng hòa vốn";
}

public class FefoAlertItemDto
{
    public Guid StockId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string? WarehouseName { get; set; }
    public int OnhandQty { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public string AlertLevel { get; set; } = "Red"; // "Red", "Orange", "Yellow"
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public bool HasExpired { get; set; }
    public string StatusText { get; set; } = string.Empty;
}
