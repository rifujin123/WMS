using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Application.Mappings;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Services;
using WMS.UnitTests.Common;
using Xunit;

namespace WMS.UnitTests.Analytics;

public class WorkingCapitalRadarTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _warehouseId1 = Guid.NewGuid();
    private readonly Guid _warehouseId2 = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();

    private void SeedTenantAndWarehouses(WmsDbContext db, Tenant? customTenant = null)
    {
        var tenant = customTenant ?? new Tenant
        {
            Id = _tenantId,
            Name = "Công ty TNHH WMS Test",
            Code = "wmstest",
            NearExpiryDays = 30,
            DeadStockDays = 90,
            FrozenCapitalDays = 60,
            EnableExpiryAlertBanner = true,
            IsActive = true
        };
        db.Tenants.Add(tenant);

        db.Categories.Add(new Category
        {
            Id = _categoryId,
            TenantId = _tenantId,
            Name = "Danh mục mặc định"
        });

        db.Warehouses.AddRange(
            new Warehouse { Id = _warehouseId1, TenantId = _tenantId, Name = "Kho Hà Nội", Code = "WH-HN" },
            new Warehouse { Id = _warehouseId2, TenantId = _tenantId, Name = "Kho HCM", Code = "WH-HCM" }
        );
        db.SaveChanges();
    }

    [Fact]
    public async Task GetRadarData_CalculatesAgingBucketsCorrectly()
    {
        var db = TestDbContextFactory.Create(_tenantId);
        SeedTenantAndWarehouses(db);

        var loc = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            WarehouseId = _warehouseId1,
            Code = "LOC-01",
            MaxQuantity = 1000
        };
        db.Locations.Add(loc);

        var p1 = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, CategoryId = _categoryId, Sku = "SKU-01", Name = "Sản phẩm 1", Price = 100000, PurchasePrice = 60000 };
        var p2 = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, CategoryId = _categoryId, Sku = "SKU-02", Name = "Sản phẩm 2", Price = 200000, PurchasePrice = 120000 };
        var p3 = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, CategoryId = _categoryId, Sku = "SKU-03", Name = "Sản phẩm 3", Price = 300000, PurchasePrice = 200000 };
        var p4 = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, CategoryId = _categoryId, Sku = "SKU-04", Name = "Sản phẩm 4", Price = 400000, PurchasePrice = 250000 };
        db.Products.AddRange(p1, p2, p3, p4);

        var now = DateTime.UtcNow;
        // 4 stocks with different ages: 10d (0-30), 40d (31-60), 75d (61-90), 120d (>90)
        db.Stocks.AddRange(
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = p1.Id, LocationId = loc.Id, OnhandQty = 10, InboundDate = now.AddDays(-10) }, // 10 * 60k = 600k
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = p2.Id, LocationId = loc.Id, OnhandQty = 5, InboundDate = now.AddDays(-40) },  // 5 * 120k = 600k
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = p3.Id, LocationId = loc.Id, OnhandQty = 4, InboundDate = now.AddDays(-75) },  // 4 * 200k = 800k
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = p4.Id, LocationId = loc.Id, OnhandQty = 2, InboundDate = now.AddDays(-120) }  // 2 * 250k = 500k
        );
        db.SaveChanges();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.TenantId).Returns(_tenantId);
        mockUser.Setup(u => u.WarehouseId).Returns((Guid?)null);

        var service = new WorkingCapitalRadarService(db, mockUser.Object);
        var result = await service.GetRadarDataAsync();

        Assert.Equal(2500000m, result.TotalWorkingCapital); // 600k + 600k + 800k + 500k
        Assert.Equal(1300000m, result.TotalFrozenCapital);  // 75d (800k) + 120d (500k) (>60 days)
        Assert.Equal(500000m, result.DeadStockCapital);     // 120d (500k) (>90 days)

        Assert.Equal(4, result.AgingBuckets.Count);
        Assert.Equal(600000m, result.AgingBuckets[0].TotalValue); // 0-30
        Assert.Equal(10, result.AgingBuckets[0].Quantity);
        Assert.Equal(24.00m, result.AgingBuckets[0].Percentage);

        Assert.Equal(600000m, result.AgingBuckets[1].TotalValue); // 31-60
        Assert.Equal(5, result.AgingBuckets[1].Quantity);

        Assert.Equal(800000m, result.AgingBuckets[2].TotalValue); // 61-90
        Assert.Equal(4, result.AgingBuckets[2].Quantity);

        Assert.Equal(500000m, result.AgingBuckets[3].TotalValue); // >90
        Assert.Equal(2, result.AgingBuckets[3].Quantity);
    }

    [Fact]
    public async Task GetRadarData_FallbackToPriceSeventyPercent_WhenPurchasePriceZero()
    {
        var db = TestDbContextFactory.Create(_tenantId);
        SeedTenantAndWarehouses(db);

        var loc = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            WarehouseId = _warehouseId1,
            Code = "LOC-02",
            MaxQuantity = 500
        };
        db.Locations.Add(loc);

        // Product without PurchasePrice: Price = 1,000,000, Fallback cost = 700,000
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CategoryId = _categoryId,
            Sku = "SKU-NOPRICE",
            Name = "Sản phẩm không có giá vốn",
            Price = 1000000m,
            PurchasePrice = 0m
        };
        db.Products.Add(product);

        db.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            LocationId = loc.Id,
            OnhandQty = 10,
            InboundDate = DateTime.UtcNow.AddDays(-100) // Dead stock
        });
        db.SaveChanges();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.TenantId).Returns(_tenantId);

        var service = new WorkingCapitalRadarService(db, mockUser.Object);
        var result = await service.GetRadarDataAsync();

        // 10 * 700,000 = 7,000,000
        Assert.Equal(7000000m, result.TotalWorkingCapital);
        Assert.Equal(7000000m, result.DeadStockCapital);
        Assert.Single(result.TopDeadStocks);
        Assert.Equal(700000m, result.TopDeadStocks[0].PurchasePrice);
        Assert.Equal(700000m, result.TopDeadStocks[0].BreakEvenPrice);
        Assert.Equal(7000000m, result.TopDeadStocks[0].TiedUpCapital);
    }

    [Fact]
    public async Task GetRadarData_FallbackToCreatedDate_WhenInboundDateIsNull()
    {
        var db = TestDbContextFactory.Create(_tenantId);
        SeedTenantAndWarehouses(db);

        var loc = new Location { Id = Guid.NewGuid(), TenantId = _tenantId, WarehouseId = _warehouseId1, Code = "LOC-03", MaxQuantity = 500 };
        db.Locations.Add(loc);

        var product = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, CategoryId = _categoryId, Sku = "SKU-LEGACY", Name = "Hàng tồn cũ", Price = 200000, PurchasePrice = 150000 };
        db.Products.Add(product);

        // Stock before migration: InboundDate == null, CreatedDate is 75 days ago
        var stock = new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            LocationId = loc.Id,
            OnhandQty = 8,
            InboundDate = null,
            CreatedDate = DateTime.UtcNow.AddDays(-75)
        };
        db.Stocks.Add(stock);
        db.SaveChanges();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.TenantId).Returns(_tenantId);

        var service = new WorkingCapitalRadarService(db, mockUser.Object);
        var result = await service.GetRadarDataAsync();

        // 8 * 150k = 1,200,000
        Assert.Equal(1200000m, result.TotalWorkingCapital);
        Assert.Equal(1200000m, result.TotalFrozenCapital); // 75 days > 60 days
        Assert.Equal(0m, result.DeadStockCapital);        // 75 days <= 90 days
        Assert.Equal(1200000m, result.AgingBuckets[2].TotalValue); // 61-90 bucket
    }

    [Fact]
    public async Task GetRadarData_FefoAlertLevels_RedOrangeYellowAndExpired()
    {
        var db = TestDbContextFactory.Create(_tenantId);
        // Tenant with NearExpiryDays = 45
        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Công ty Dược phẩm",
            Code = "pharma",
            NearExpiryDays = 45,
            DeadStockDays = 90,
            FrozenCapitalDays = 60,
            EnableExpiryAlertBanner = true,
            IsActive = true
        };
        SeedTenantAndWarehouses(db, tenant);

        var loc = new Location { Id = Guid.NewGuid(), TenantId = _tenantId, WarehouseId = _warehouseId1, Code = "LOC-FEFO", MaxQuantity = 500 };
        db.Locations.Add(loc);

        var product = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, CategoryId = _categoryId, Sku = "MED-01", Name = "Thuốc kháng sinh", Price = 50000, PurchasePrice = 30000 };
        db.Products.Add(product);

        var today = DateTime.UtcNow.Date;
        db.Stocks.AddRange(
            // Expired 2 days ago (<0) -> Red
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = product.Id, LocationId = loc.Id, OnhandQty = 5, ExpiryDate = today.AddDays(-2), InboundDate = today.AddDays(-10) },
            // Remaining 10 days (<15) -> Red
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = product.Id, LocationId = loc.Id, OnhandQty = 10, ExpiryDate = today.AddDays(10), InboundDate = today.AddDays(-10) },
            // Remaining 20 days (15-30) -> Orange
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = product.Id, LocationId = loc.Id, OnhandQty = 15, ExpiryDate = today.AddDays(20), InboundDate = today.AddDays(-10) },
            // Remaining 40 days (30-45) -> Yellow
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = product.Id, LocationId = loc.Id, OnhandQty = 20, ExpiryDate = today.AddDays(40), InboundDate = today.AddDays(-10) },
            // Remaining 60 days (> 45) -> Not in alert
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = product.Id, LocationId = loc.Id, OnhandQty = 50, ExpiryDate = today.AddDays(60), InboundDate = today.AddDays(-10) }
        );
        db.SaveChanges();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.TenantId).Returns(_tenantId);

        var service = new WorkingCapitalRadarService(db, mockUser.Object);
        var result = await service.GetRadarDataAsync();

        Assert.Equal(4, result.NearExpiryStockCount);
        Assert.True(result.HasEmergencyAlert);
        Assert.Equal(2, result.EmergencyAlertCount); // Expired (-2) and 10 days (<15)

        // Verify sorted: expired first
        Assert.True(result.FefoAlerts[0].HasExpired);
        Assert.Equal("Red", result.FefoAlerts[0].AlertLevel);
        Assert.Equal("Đã hết hạn (2 ngày)", result.FefoAlerts[0].StatusText);

        Assert.Equal("Red", result.FefoAlerts[1].AlertLevel);
        Assert.Equal("Còn 10 ngày", result.FefoAlerts[1].StatusText);

        Assert.Equal("Orange", result.FefoAlerts[2].AlertLevel);
        Assert.Equal("Còn 20 ngày", result.FefoAlerts[2].StatusText);

        Assert.Equal("Yellow", result.FefoAlerts[3].AlertLevel);
        Assert.Equal("Còn 40 ngày", result.FefoAlerts[3].StatusText);
    }

    [Fact]
    public async Task GetRadarData_WarehouseScoping_FiltersCorrectly()
    {
        var db = TestDbContextFactory.Create(_tenantId);
        SeedTenantAndWarehouses(db);

        var locWh1 = new Location { Id = Guid.NewGuid(), TenantId = _tenantId, WarehouseId = _warehouseId1, Code = "WH1-L1", MaxQuantity = 500 };
        var locWh2 = new Location { Id = Guid.NewGuid(), TenantId = _tenantId, WarehouseId = _warehouseId2, Code = "WH2-L1", MaxQuantity = 500 };
        db.Locations.AddRange(locWh1, locWh2);

        var product = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, CategoryId = _categoryId, Sku = "SKU-SCOPE", Name = "Hàng 2 kho", Price = 100000, PurchasePrice = 80000 };
        db.Products.Add(product);

        db.Stocks.AddRange(
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = product.Id, LocationId = locWh1.Id, OnhandQty = 10, InboundDate = DateTime.UtcNow.AddDays(-20) }, // 10 * 80k = 800k in WH1
            new Stock { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = product.Id, LocationId = locWh2.Id, OnhandQty = 20, InboundDate = DateTime.UtcNow.AddDays(-20) }  // 20 * 80k = 1.6m in WH2
        );
        db.SaveChanges();

        // Scenario 1: User scoped to WH1
        var mockManagerWh1 = new Mock<ICurrentUserService>();
        mockManagerWh1.Setup(u => u.TenantId).Returns(_tenantId);
        mockManagerWh1.Setup(u => u.WarehouseId).Returns(_warehouseId1);

        var serviceWh1 = new WorkingCapitalRadarService(db, mockManagerWh1.Object);
        var resultWh1 = await serviceWh1.GetRadarDataAsync();
        Assert.Equal(800000m, resultWh1.TotalWorkingCapital);
        Assert.Equal("Kho Hà Nội", resultWh1.WarehouseName);

        // Scenario 2: Admin scoped to all
        var mockAdmin = new Mock<ICurrentUserService>();
        mockAdmin.Setup(u => u.TenantId).Returns(_tenantId);
        mockAdmin.Setup(u => u.WarehouseId).Returns((Guid?)null);

        var serviceAdmin = new WorkingCapitalRadarService(db, mockAdmin.Object);
        var resultAll = await serviceAdmin.GetRadarDataAsync();
        Assert.Equal(2400000m, resultAll.TotalWorkingCapital);
        Assert.Null(resultAll.WarehouseName);

        // Scenario 3: Admin filters query param for WH2
        var resultWh2 = await serviceAdmin.GetRadarDataAsync(_warehouseId2);
        Assert.Equal(1600000m, resultWh2.TotalWorkingCapital);
        Assert.Equal("Kho HCM", resultWh2.WarehouseName);
    }

    [Fact]
    public async Task PutAwayService_CompleteAsync_SetsInboundDateOnStock()
    {
        var db = TestDbContextFactory.Create(_tenantId);
        SeedTenantAndWarehouses(db);

        var loc = new Location { Id = Guid.NewGuid(), TenantId = _tenantId, WarehouseId = _warehouseId1, Code = "LOC-PUTAWAY", MaxQuantity = 500, CurrentQuantity = 0, LocationType = LocationType.Storage };
        db.Locations.Add(loc);

        var product = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, Sku = "SKU-PUTAWAY", Name = "Hàng cất kho", Price = 50000, PurchasePrice = 30000 };
        db.Products.Add(product);

        var po = new PurchaseOrder { Id = Guid.NewGuid(), TenantId = _tenantId, PoNumber = "PO-01", WarehouseId = _warehouseId1, Status = PurchaseOrderStatus.Approved };
        db.PurchaseOrders.Add(po);

        var receiving = new Receiving { Id = Guid.NewGuid(), TenantId = _tenantId, ReceivingNo = "REC-01", PurchaseOrderId = po.Id, Status = ReceivingStatus.Confirmed };
        var recDetail = new ReceivingDetail { Id = Guid.NewGuid(), TenantId = _tenantId, ReceivingId = receiving.Id, ProductId = product.Id, ActualQuantity = 20, LotNumber = "LOT-1", ExpiryDate = DateTime.UtcNow.AddMonths(6) };
        receiving.ReceivingDetails.Add(recDetail);
        db.Receivings.Add(receiving);

        var task = new PutAwayTask
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ReceivingDetailId = recDetail.Id,
            ProductId = product.Id,
            Quantity = 10,
            FromLocationId = loc.Id,
            ToLocationId = loc.Id,
            Status = PutAwayTaskStatus.InProgress,
            AssignToId = Guid.NewGuid()
        };
        db.PutAwayTasks.Add(task);
        db.SaveChanges();

        var mockTaskRepo = new Mock<IPutAwayTaskRepository>();
        mockTaskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);

        var mockRecRepo = new Mock<IReceivingRepository>();
        mockRecRepo.Setup(r => r.GetDetailByIdAsync(recDetail.Id)).ReturnsAsync(recDetail);

        var mockLocRepo = new Mock<ILocationRepository>();
        mockLocRepo.Setup(r => r.GetByIdAsync(loc.Id)).ReturnsAsync(loc);

        var mockStockRepo = new Mock<IStockRepository>();
        Stock? capturedStock = null;
        mockStockRepo.Setup(r => r.GetByProductAndLocationAsync(product.Id, loc.Id, recDetail.LotNumber, recDetail.ExpiryDate))
            .ReturnsAsync((Stock?)null);
        mockStockRepo.Setup(r => r.AddAsync(It.IsAny<Stock>()))
            .Callback<Stock>(s => capturedStock = s)
            .Returns(Task.CompletedTask);

        var mockMovementRepo = new Mock<IStockMovementRepository>();
        var mockPoRepo = new Mock<IPurchaseOrderRepository>();
        var mockUserManager = new Mock<UserManager<User>>(Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(u => u.UserId).Returns(task.AssignToId);
        mockCurrentUser.Setup(u => u.IsInRole(new[] { "Admin", "WarehouseManager" })).Returns(true);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var service = new PutAwayService(
            mockTaskRepo.Object,
            mockRecRepo.Object,
            mockPoRepo.Object,
            mockStockRepo.Object,
            mockMovementRepo.Object,
            mockLocRepo.Object,
            mockUserManager.Object,
            mockUnitOfWork.Object,
            mockCurrentUser.Object,
            mapper
        );

        var result = await service.CompleteAsync(task.Id);

        Assert.NotNull(capturedStock);
        Assert.NotNull(capturedStock.InboundDate);
        Assert.True((DateTime.UtcNow - capturedStock.InboundDate.Value).TotalSeconds < 5);
    }

    [Fact]
    public async Task PutAwayService_CompleteAsync_ExistingStock_SetsInboundDateWhenNull()
    {
        var db = TestDbContextFactory.Create(_tenantId);
        SeedTenantAndWarehouses(db);

        var loc = new Location { Id = Guid.NewGuid(), TenantId = _tenantId, WarehouseId = _warehouseId1, Code = "LOC-EXIST", MaxQuantity = 500, CurrentQuantity = 10, LocationType = LocationType.Storage };
        db.Locations.Add(loc);

        var product = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, Sku = "SKU-EXIST", Name = "Hàng tồn có sẵn", Price = 50000, PurchasePrice = 30000 };
        db.Products.Add(product);

        var po = new PurchaseOrder { Id = Guid.NewGuid(), TenantId = _tenantId, PoNumber = "PO-02", WarehouseId = _warehouseId1, Status = PurchaseOrderStatus.Approved };
        db.PurchaseOrders.Add(po);

        var receiving = new Receiving { Id = Guid.NewGuid(), TenantId = _tenantId, ReceivingNo = "REC-02", PurchaseOrderId = po.Id, Status = ReceivingStatus.Confirmed };
        var recDetail = new ReceivingDetail { Id = Guid.NewGuid(), TenantId = _tenantId, ReceivingId = receiving.Id, ProductId = product.Id, ActualQuantity = 10 };
        receiving.ReceivingDetails.Add(recDetail);
        db.Receivings.Add(receiving);

        var task = new PutAwayTask
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ReceivingDetailId = recDetail.Id,
            ProductId = product.Id,
            Quantity = 10,
            FromLocationId = loc.Id,
            ToLocationId = loc.Id,
            Status = PutAwayTaskStatus.InProgress,
            AssignToId = Guid.NewGuid()
        };
        db.PutAwayTasks.Add(task);

        var existingStock = new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            LocationId = loc.Id,
            OnhandQty = 10,
            InboundDate = null // Legacy stock
        };
        db.Stocks.Add(existingStock);
        db.SaveChanges();

        var mockTaskRepo = new Mock<IPutAwayTaskRepository>();
        mockTaskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);

        var mockRecRepo = new Mock<IReceivingRepository>();
        mockRecRepo.Setup(r => r.GetDetailByIdAsync(recDetail.Id)).ReturnsAsync(recDetail);

        var mockLocRepo = new Mock<ILocationRepository>();
        mockLocRepo.Setup(r => r.GetByIdAsync(loc.Id)).ReturnsAsync(loc);

        var mockStockRepo = new Mock<IStockRepository>();
        mockStockRepo.Setup(r => r.GetByProductAndLocationAsync(product.Id, loc.Id, null, null))
            .ReturnsAsync(existingStock);

        var mockMovementRepo = new Mock<IStockMovementRepository>();
        var mockPoRepo = new Mock<IPurchaseOrderRepository>();
        var mockUserManager = new Mock<UserManager<User>>(Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(u => u.UserId).Returns(task.AssignToId);
        mockCurrentUser.Setup(u => u.IsInRole(new[] { "Admin", "WarehouseManager" })).Returns(true);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var service = new PutAwayService(
            mockTaskRepo.Object,
            mockRecRepo.Object,
            mockPoRepo.Object,
            mockStockRepo.Object,
            mockMovementRepo.Object,
            mockLocRepo.Object,
            mockUserManager.Object,
            mockUnitOfWork.Object,
            mockCurrentUser.Object,
            mapper
        );

        await service.CompleteAsync(task.Id);

        Assert.Equal(20, existingStock.OnhandQty);
        Assert.NotNull(existingStock.InboundDate);
        Assert.True((DateTime.UtcNow - existingStock.InboundDate.Value).TotalSeconds < 5);
    }
}
