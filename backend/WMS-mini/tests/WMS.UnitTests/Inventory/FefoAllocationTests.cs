using WMS.Domain.Entities;
using WMS.Infrastructure.Repositories;
using WMS.UnitTests.Common;

namespace WMS.UnitTests.Inventory;

public class FefoAllocationTests
{
    [Fact]
    public async Task FEFO_ShouldPrioritizeNearestExpiry_AndExcludeExpiredLots()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            // Seed Tenant with FEFO enabled
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Công ty Dược phẩm FEFO",
                Code = "pharma-fefo",
                HasExpiryManagement = true,
                IsActive = true
            });

            var warehouse = new Warehouse { Id = warehouseId, Name = "Kho Dược Chính", Code = "WH-PHARMA", TenantId = tenantId };
            db.Warehouses.Add(warehouse);

            var location = new Location { Id = locationId, WarehouseId = warehouseId, Code = "LOC-01", TenantId = tenantId };
            db.Locations.Add(location);

            var product = new Product { Id = productId, Name = "Kháng sinh A", Sku = "MED-01", TenantId = tenantId };
            db.Products.Add(product);

            // 1. Lô đã hết hạn hôm qua
            db.Stocks.Add(new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationId = locationId,
                TenantId = tenantId,
                OnhandQty = 50,
                ReservedQty = 0,
                LotNumber = "LOT-EXPIRED",
                ExpiryDate = DateTime.UtcNow.AddDays(-1),
                CreatedDate = DateTime.UtcNow.AddDays(-20)
            });

            // 2. Lô còn 30 ngày hết hạn
            db.Stocks.Add(new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationId = locationId,
                TenantId = tenantId,
                OnhandQty = 100,
                ReservedQty = 0,
                LotNumber = "LOT-30DAYS",
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                CreatedDate = DateTime.UtcNow.AddDays(-5)
            });

            // 3. Lô còn 10 ngày hết hạn (cận date hơn)
            db.Stocks.Add(new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationId = locationId,
                TenantId = tenantId,
                OnhandQty = 80,
                ReservedQty = 0,
                LotNumber = "LOT-10DAYS",
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            });

            await db.SaveChangesAsync();
        }

        // Act
        using (var dbQuery = TestDbContextFactory.Create(tenantId, dbName))
        {
            var repo = new SqlStockRepository(dbQuery);
            var availableStocks = await repo.GetAvailableByProductAndWarehouseAsync(productId, warehouseId);

            // Assert: Lô hết hạn bị loại trừ, lô 10 ngày đứng trước lô 30 ngày
            Assert.Equal(2, availableStocks.Count);
            Assert.Equal("LOT-10DAYS", availableStocks[0].LotNumber);
            Assert.Equal("LOT-30DAYS", availableStocks[1].LotNumber);
        }
    }

    [Fact]
    public async Task FIFO_ShouldPrioritizeOldestCreatedDate_RegardlessOfExpiry()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            // Seed Tenant với FIFO tiêu chuẩn
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Công ty Thiết bị Điện tử",
                Code = "elec-fifo",
                HasExpiryManagement = false,
                IsActive = true
            });

            var warehouse = new Warehouse { Id = warehouseId, Name = "Kho Thiết Bị", Code = "WH-ELEC", TenantId = tenantId };
            db.Warehouses.Add(warehouse);

            var location = new Location { Id = locationId, WarehouseId = warehouseId, Code = "LOC-02", TenantId = tenantId };
            db.Locations.Add(location);

            var product = new Product { Id = productId, Name = "Chip Bán Dẫn", Sku = "CHIP-01", TenantId = tenantId };
            db.Products.Add(product);

            // Lô mới tạo 1 ngày trước
            db.Stocks.Add(new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationId = locationId,
                TenantId = tenantId,
                OnhandQty = 50,
                ReservedQty = 0,
                LotNumber = "NEW-BATCH",
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            });

            // Lô cũ tạo 10 ngày trước
            db.Stocks.Add(new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationId = locationId,
                TenantId = tenantId,
                OnhandQty = 30,
                ReservedQty = 0,
                LotNumber = "OLD-BATCH",
                CreatedDate = DateTime.UtcNow.AddDays(-10)
            });

            await db.SaveChangesAsync();
        }

        // Act
        using (var dbQuery = TestDbContextFactory.Create(tenantId, dbName))
        {
            var repo = new SqlStockRepository(dbQuery);
            var availableStocks = await repo.GetAvailableByProductAndWarehouseAsync(productId, warehouseId);

            // Assert: Lô cũ (OLD-BATCH) được ưu tiên lấy trước
            Assert.Equal(2, availableStocks.Count);
            Assert.Equal("OLD-BATCH", availableStocks[0].LotNumber);
            Assert.Equal("NEW-BATCH", availableStocks[1].LotNumber);
        }
    }

    [Fact]
    public async Task StockAvailability_ShouldExcludeFullyReservedStock()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            var warehouse = new Warehouse { Id = warehouseId, Name = "Kho A", Code = "WH-A", TenantId = tenantId };
            db.Warehouses.Add(warehouse);

            var location = new Location { Id = locationId, WarehouseId = warehouseId, Code = "LOC-03", TenantId = tenantId };
            db.Locations.Add(location);

            // Lô 1: Tồn 50, Đã khóa 50 => Khả dụng = 0
            db.Stocks.Add(new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationId = locationId,
                TenantId = tenantId,
                OnhandQty = 50,
                ReservedQty = 50
            });

            // Lô 2: Tồn 100, Đã khóa 20 => Khả dụng = 80
            db.Stocks.Add(new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationId = locationId,
                TenantId = tenantId,
                OnhandQty = 100,
                ReservedQty = 20
            });

            await db.SaveChangesAsync();
        }

        // Act
        using (var dbQuery = TestDbContextFactory.Create(tenantId, dbName))
        {
            var repo = new SqlStockRepository(dbQuery);
            var availableStocks = await repo.GetAvailableByProductAndWarehouseAsync(productId, warehouseId);

            // Assert: Chỉ trả về Lô 2
            Assert.Single(availableStocks);
            Assert.Equal(100, availableStocks[0].OnhandQty);
            Assert.Equal(20, availableStocks[0].ReservedQty);
        }
    }
}
