namespace WMS.Application.DTOs;

/// Kết quả chạy seed dữ liệu demo.
/// Các count theo nhóm được các ticket sau điền dần (users, warehouse, stock, document…).
public sealed record SeedSummary
{
    public bool Skipped { get; init; }
    public bool AlreadySeeded { get; init; }

    public int Users { get; init; }
    public int Warehouses { get; init; }
    public int Locations { get; init; }
    public int Categories { get; init; }
    public int Products { get; init; }
    public int Vendors { get; init; }
    public int Customers { get; init; }
    public int StockMovements { get; init; }
    public int PurchaseOrders { get; init; }
    public int SaleOrders { get; init; }
    public int StockAdjustments { get; init; }

    public static SeedSummary Disabled() => new() { Skipped = true };
    public static SeedSummary AlreadySeededSummary() => new() { AlreadySeeded = true };
}