using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class WorkingCapitalRadarService : IWorkingCapitalRadarService
{
    private readonly WmsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public WorkingCapitalRadarService(WmsDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<WorkingCapitalRadarDto> GetRadarDataAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUser.TenantId ?? _db.CurrentTenantId;
        var tenant = tenantId.HasValue
            ? await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId.Value, cancellationToken)
            : null;

        var nearExpiryDays = tenant != null && tenant.NearExpiryDays > 0 ? tenant.NearExpiryDays : 30;
        var deadStockDays = tenant != null && tenant.DeadStockDays > 0 ? tenant.DeadStockDays : 90;
        var frozenCapitalDays = tenant != null && tenant.FrozenCapitalDays > 0 ? tenant.FrozenCapitalDays : 60;
        var enableExpiryAlertBanner = tenant == null || tenant.EnableExpiryAlertBanner;

        var effectiveWarehouseId = _currentUser.WarehouseId ?? warehouseId;
        string? warehouseName = null;

        if (effectiveWarehouseId.HasValue)
        {
            var wh = await _db.Warehouses
                .Where(w => w.Id == effectiveWarehouseId.Value)
                .Select(w => new { w.Name })
                .FirstOrDefaultAsync(cancellationToken);
            warehouseName = wh?.Name;
        }

        var stockQuery = _db.Stocks
            .AsNoTracking()
            .Include(s => s.Product)
                .ThenInclude(p => p.Category)
            .Include(s => s.Location)
                .ThenInclude(l => l.Warehouse)
            .Where(s => s.OnhandQty > 0);

        if (effectiveWarehouseId.HasValue)
        {
            stockQuery = stockQuery.Where(s => s.Location.WarehouseId == effectiveWarehouseId.Value);
        }

        var stocks = await stockQuery.ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        decimal totalWorkingCapital = 0m;
        decimal totalFrozenCapital = 0m;
        decimal deadStockCapital = 0m;

        var bucket0To30Value = 0m;
        var bucket0To30Qty = 0;
        var bucket31To60Value = 0m;
        var bucket31To60Qty = 0;
        var bucket61To90Value = 0m;
        var bucket61To90Qty = 0;
        var bucketOver90Value = 0m;
        var bucketOver90Qty = 0;

        var deadStockItems = new List<(Stock Stock, decimal UnitCost, decimal Value, int DaysInStock)>();
        var fefoAlertItems = new List<FefoAlertItemDto>();

        foreach (var stock in stocks)
        {
            var inboundDate = stock.InboundDate ?? stock.CreatedDate;
            var daysInStockDouble = Math.Max(0, (now - inboundDate).TotalDays);
            var daysInStock = (int)Math.Floor(daysInStockDouble);

            var unitCost = stock.Product.PurchasePrice > 0
                ? stock.Product.PurchasePrice
                : stock.Product.Price * 0.7m;

            var stockValue = stock.OnhandQty * unitCost;
            totalWorkingCapital += stockValue;

            if (daysInStockDouble > frozenCapitalDays)
            {
                totalFrozenCapital += stockValue;
            }

            if (daysInStockDouble > deadStockDays)
            {
                deadStockCapital += stockValue;
                deadStockItems.Add((stock, unitCost, stockValue, daysInStock));
            }

            // Phân bổ 4 nhóm Aging Buckets (0-30, 31-60, 61-90, > 90)
            if (daysInStockDouble <= 30)
            {
                bucket0To30Value += stockValue;
                bucket0To30Qty += stock.OnhandQty;
            }
            else if (daysInStockDouble <= 60)
            {
                bucket31To60Value += stockValue;
                bucket31To60Qty += stock.OnhandQty;
            }
            else if (daysInStockDouble <= 90)
            {
                bucket61To90Value += stockValue;
                bucket61To90Qty += stock.OnhandQty;
            }
            else
            {
                bucketOver90Value += stockValue;
                bucketOver90Qty += stock.OnhandQty;
            }

            // Quét cảnh báo FEFO cận hạn
            if (stock.ExpiryDate.HasValue)
            {
                var daysRemaining = (int)Math.Floor((stock.ExpiryDate.Value.Date - now.Date).TotalDays);
                if (daysRemaining <= nearExpiryDays)
                {
                    string alertLevel;
                    if (daysRemaining < 15) alertLevel = "Red";
                    else if (daysRemaining <= 30) alertLevel = "Orange";
                    else alertLevel = "Yellow";

                    var hasExpired = daysRemaining < 0;
                    string statusText;
                    if (hasExpired)
                    {
                        statusText = $"Đã hết hạn ({Math.Abs(daysRemaining)} ngày)";
                    }
                    else if (daysRemaining == 0)
                    {
                        statusText = "Hết hạn hôm nay";
                    }
                    else
                    {
                        statusText = $"Còn {daysRemaining} ngày";
                    }

                    fefoAlertItems.Add(new FefoAlertItemDto
                    {
                        StockId = stock.Id,
                        ProductId = stock.ProductId,
                        ProductSku = stock.Product.Sku,
                        ProductName = stock.Product.Name,
                        LotNumber = stock.LotNumber,
                        LocationCode = stock.Location?.Code ?? "N/A",
                        WarehouseName = stock.Location?.Warehouse?.Name,
                        OnhandQty = stock.OnhandQty,
                        ExpiryDate = stock.ExpiryDate,
                        DaysRemaining = daysRemaining,
                        AlertLevel = alertLevel,
                        UnitCost = unitCost,
                        TotalValue = stockValue,
                        HasExpired = hasExpired,
                        StatusText = statusText
                    });
                }
            }
        }

        var agingBuckets = new List<AgingBucketDto>
        {
            new AgingBucketDto
            {
                Range = "0-30 ngày",
                MinDays = 0,
                MaxDays = 30,
                TotalValue = bucket0To30Value,
                Quantity = bucket0To30Qty,
                Percentage = totalWorkingCapital > 0 ? Math.Round((bucket0To30Value / totalWorkingCapital) * 100m, 2) : 0m
            },
            new AgingBucketDto
            {
                Range = "31-60 ngày",
                MinDays = 31,
                MaxDays = 60,
                TotalValue = bucket31To60Value,
                Quantity = bucket31To60Qty,
                Percentage = totalWorkingCapital > 0 ? Math.Round((bucket31To60Value / totalWorkingCapital) * 100m, 2) : 0m
            },
            new AgingBucketDto
            {
                Range = "61-90 ngày",
                MinDays = 61,
                MaxDays = 90,
                TotalValue = bucket61To90Value,
                Quantity = bucket61To90Qty,
                Percentage = totalWorkingCapital > 0 ? Math.Round((bucket61To90Value / totalWorkingCapital) * 100m, 2) : 0m
            },
            new AgingBucketDto
            {
                Range = "> 90 ngày",
                MinDays = 91,
                MaxDays = null,
                TotalValue = bucketOver90Value,
                Quantity = bucketOver90Qty,
                Percentage = totalWorkingCapital > 0 ? Math.Round((bucketOver90Value / totalWorkingCapital) * 100m, 2) : 0m
            }
        };

        // Top 5 sản phẩm chôn vốn nặng nhất
        var topDeadStocks = deadStockItems
            .GroupBy(d => d.Stock.ProductId)
            .Select(g =>
            {
                var first = g.First();
                var totalQty = g.Sum(x => x.Stock.OnhandQty);
                var totalTiedUpCapital = g.Sum(x => x.Value);
                var maxDays = g.Max(x => x.DaysInStock);
                var locations = g.Select(x => x.Stock.Location?.Code ?? "N/A").Distinct().ToList();
                var avgCost = totalQty > 0 ? Math.Round(totalTiedUpCapital / totalQty, 2) : first.UnitCost;

                string suggestion = maxDays > 180
                    ? "Xuất trả NCC hoặc thanh lý sâu"
                    : maxDays > 120
                        ? "Khuyến mãi giảm giá sốc"
                        : "Xả hàng hòa vốn";

                return new TopDeadStockItemDto
                {
                    ProductId = first.Stock.ProductId,
                    ProductSku = first.Stock.Product.Sku,
                    ProductName = first.Stock.Product.Name,
                    CategoryName = first.Stock.Product.Category?.Name,
                    Quantity = totalQty,
                    PurchasePrice = avgCost,
                    TiedUpCapital = totalTiedUpCapital,
                    BreakEvenPrice = avgCost,
                    DaysInStock = maxDays,
                    Locations = locations,
                    ActionSuggestion = suggestion
                };
            })
            .OrderByDescending(t => t.TiedUpCapital)
            .Take(5)
            .ToList();

        // FEFO Alerts sắp xếp theo hạn sử dụng tăng dần (cận nhất lên trước)
        var sortedFefoAlerts = fefoAlertItems
            .OrderBy(a => a.DaysRemaining)
            .ThenBy(a => a.ExpiryDate)
            .ToList();

        var emergencyAlerts = sortedFefoAlerts
            .Where(a => a.AlertLevel == "Red" || a.DaysRemaining < 15 || a.HasExpired)
            .ToList();

        return new WorkingCapitalRadarDto
        {
            TotalWorkingCapital = totalWorkingCapital,
            TotalFrozenCapital = totalFrozenCapital,
            DeadStockCapital = deadStockCapital,
            NearExpiryStockCount = sortedFefoAlerts.Count,
            NearExpiryStockCapital = sortedFefoAlerts.Sum(f => f.TotalValue),
            WarehouseId = effectiveWarehouseId,
            WarehouseName = warehouseName,
            NearExpiryDays = nearExpiryDays,
            DeadStockDays = deadStockDays,
            FrozenCapitalDays = frozenCapitalDays,
            EnableExpiryAlertBanner = enableExpiryAlertBanner,
            HasEmergencyAlert = enableExpiryAlertBanner && emergencyAlerts.Count > 0,
            EmergencyAlertCount = emergencyAlerts.Count,
            AgingBuckets = agingBuckets,
            TopDeadStocks = topDeadStocks,
            FefoAlerts = sortedFefoAlerts
        };
    }
}
