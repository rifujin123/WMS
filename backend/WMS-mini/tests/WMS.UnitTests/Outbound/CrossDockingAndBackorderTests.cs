using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Application.Mappings;
using WMS.Application.Services;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Repositories;
using WMS.Infrastructure.Services;
using WMS.Infrastructure.Workers;
using WMS.UnitTests.Common;
using Xunit;

namespace WMS.UnitTests.Outbound;

public class CrossDockingAndBackorderTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task ReservationTimeoutWorker_ShouldCancelExpiredOrders_AndCleanupLinks()
    {
        var tenantId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();
        var db = TestDbContextFactory.Create(tenantId, dbName);

        var product = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-EXPIRED", Name = "Expired Prod" };
        var po = new PurchaseOrder { Id = Guid.NewGuid(), TenantId = tenantId, PoNumber = "PO-EXP-01", Status = PurchaseOrderStatus.Approved };
        var poDetail = new PurchaseOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderId = po.Id, ProductId = product.Id, OrderedQuantity = 10 };
        po.PurchaseOrderDetails.Add(poDetail);

        var so = new SaleOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNo = "SO-TIMEOUT",
            Status = SaleOrderStatus.AwaitingDeposit,
            DepositDeadline = DateTime.UtcNow.AddMinutes(-10),
            IsPreOrder = true,
            RequiredDeposit = 500000m,
            PaidDeposit = 0m
        };
        var soDetail = new SaleOrderDetail
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SaleOrderId = so.Id,
            ProductId = product.Id,
            Quantity = 5
        };
        so.SaleOrderDetails.Add(soDetail);

        var link = new BackorderLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SaleOrderDetailId = soDetail.Id,
            PurchaseOrderDetailId = poDetail.Id,
            Quantity = 5,
            FulfilledQuantity = 0,
            IsFulfilled = false
        };

        db.Products.Add(product);
        db.PurchaseOrders.Add(po);
        db.SaleOrders.Add(so);
        db.BackorderLinks.Add(link);
        await db.SaveChangesAsync();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => TestDbContextFactory.Create(tenantId, dbName));
        var scopeFactory = serviceCollection.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<ReservationTimeoutWorker>>();

        var worker = new ReservationTimeoutWorker(scopeFactory, loggerMock.Object);

        // Act
        var count = await worker.ProcessExpiredReservationsAsync();

        // Assert
        Assert.Equal(1, count);
        var updatedSo = await db.SaleOrders.AsNoTracking().IgnoreQueryFilters().FirstAsync(s => s.Id == so.Id);
        Assert.Equal(SaleOrderStatus.Cancelled, updatedSo.Status);
        Assert.Contains("Quá hạn nộp tiền cọc", updatedSo.DepositNotes);

        var remainingActiveLinks = await db.BackorderLinks.AsNoTracking().Where(b => b.SaleOrderDetailId == soDetail.Id).ToListAsync();
        Assert.Empty(remainingActiveLinks);
    }

    [Fact]
    public async Task CreateBackorderLink_ShouldValidatePreconditions()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId);
        var mapper = CreateMapper();

        var warehouseA = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Code = "WH-A", Name = "Kho A" };
        var warehouseB = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Code = "WH-B", Name = "Kho B" };
        var product = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-LINK-01", Name = "Sản phẩm Link" };
        var otherProduct = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-OTHER", Name = "Sản phẩm khác" };

        var po = new PurchaseOrder { Id = Guid.NewGuid(), TenantId = tenantId, PoNumber = "PO-001", WarehouseId = warehouseA.Id, Status = PurchaseOrderStatus.Approved };
        var poDetail = new PurchaseOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderId = po.Id, ProductId = product.Id, OrderedQuantity = 10 };
        po.PurchaseOrderDetails.Add(poDetail);

        var soAwaiting = new SaleOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNo = "SO-AWAIT",
            WarehouseId = warehouseA.Id,
            Status = SaleOrderStatus.AwaitingDeposit,
            IsPreOrder = true
        };
        var soAwaitingDetail = new SaleOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, SaleOrderId = soAwaiting.Id, ProductId = product.Id, Quantity = 5 };
        soAwaiting.SaleOrderDetails.Add(soAwaitingDetail);

        var soBackorder = new SaleOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNo = "SO-BACK",
            WarehouseId = warehouseA.Id,
            Status = SaleOrderStatus.Backorder,
            IsPreOrder = true
        };
        var soBackorderDetail = new SaleOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, SaleOrderId = soBackorder.Id, ProductId = product.Id, Quantity = 5 };
        soBackorder.SaleOrderDetails.Add(soBackorderDetail);

        db.Warehouses.AddRange(warehouseA, warehouseB);
        db.Products.AddRange(product, otherProduct);
        db.PurchaseOrders.Add(po);
        db.SaleOrders.AddRange(soAwaiting, soBackorder);
        await db.SaveChangesAsync();

        var linkRepo = new SqlBackorderLinkRepository(db);
        var soRepo = new SqlSaleOrderRepository(db);
        var poRepo = new SqlPurchaseOrderRepository(db);
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(() => db.SaveChangesAsync());

        var service = new BackorderLinkService(linkRepo, soRepo, poRepo, uowMock.Object, mapper);

        // 1. Cannot link when SO is in AwaitingDeposit
        var exAwaiting = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateBackorderLinkDto
            {
                SaleOrderDetailId = soAwaitingDetail.Id,
                PurchaseOrderDetailId = poDetail.Id,
                Quantity = 5
            }));
        Assert.Contains("Chỉ được gán nguồn hàng khi khách đã xác nhận nộp tiền cọc", exAwaiting.Message);

        // 2. Cannot link quantity greater than needed
        var exOverQty = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateBackorderLinkDto
            {
                SaleOrderDetailId = soBackorderDetail.Id,
                PurchaseOrderDetailId = poDetail.Id,
                Quantity = 10
            }));
        Assert.Contains("vượt quá số lượng cần bù", exOverQty.Message);

        // 3. Successful link creation
        var created = await service.CreateAsync(new CreateBackorderLinkDto
        {
            SaleOrderDetailId = soBackorderDetail.Id,
            PurchaseOrderDetailId = poDetail.Id,
            Quantity = 4
        });
        Assert.NotNull(created);
        Assert.Equal(4, created.Quantity);
        Assert.Equal(0, created.FulfilledQuantity);
        Assert.False(created.IsFulfilled);
    }

    [Fact]
    public async Task ReceivingConfirm_ShouldSplitCrossDockAndStorage_AndAllocateSaleOrder()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId);
        var mapper = CreateMapper();

        var warehouse = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Code = "WH-CROSS", Name = "Kho CrossDock" };
        var crossDockLoc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = warehouse.Id, Code = "XD-01", LocationType = LocationType.CrossDock, MaxQuantity = 1000 };
        var storageLoc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = warehouse.Id, Code = "ST-01", LocationType = LocationType.Storage, MaxQuantity = 1000 };
        var product = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-SPLIT", Name = "Sản phẩm chia task" };

        var po = new PurchaseOrder { Id = Guid.NewGuid(), TenantId = tenantId, PoNumber = "PO-SPLIT-01", WarehouseId = warehouse.Id, Status = PurchaseOrderStatus.Approved };
        var poDetail = new PurchaseOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderId = po.Id, ProductId = product.Id, OrderedQuantity = 10 };
        po.PurchaseOrderDetails.Add(poDetail);

        var so = new SaleOrder { Id = Guid.NewGuid(), TenantId = tenantId, OrderNo = "SO-SPLIT-01", WarehouseId = warehouse.Id, Status = SaleOrderStatus.Backorder, IsPreOrder = true };
        var soDetail = new SaleOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, SaleOrderId = so.Id, ProductId = product.Id, Quantity = 4, AllocatedQty = 0 };
        so.SaleOrderDetails.Add(soDetail);

        var link = new BackorderLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SaleOrderDetailId = soDetail.Id,
            PurchaseOrderDetailId = poDetail.Id,
            Quantity = 4,
            FulfilledQuantity = 0,
            IsFulfilled = false
        };

        db.Warehouses.Add(warehouse);
        db.Locations.AddRange(crossDockLoc, storageLoc);
        db.Products.Add(product);
        db.PurchaseOrders.Add(po);
        db.SaleOrders.Add(so);
        db.BackorderLinks.Add(link);
        await db.SaveChangesAsync();

        var receiving = new Receiving
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReceivingNo = "RC-SPLIT-01",
            PurchaseOrderId = po.Id,
            Status = ReceivingStatus.Draft
        };
        var rDetail = new ReceivingDetail
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReceivingId = receiving.Id,
            ProductId = product.Id,
            ExpectedQuantity = 10,
            ActualQuantity = 10,
            Condition = ProductCondition.Ok
        };
        receiving.ReceivingDetails.Add(rDetail);
        db.Receivings.Add(receiving);
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns(true);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(() => db.SaveChangesAsync());
        uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        var receivingRepo = new SqlReceivingRepository(db);
        var putAwayRepo = new SqlPutAwayTaskRepository(db);
        var poRepo = new SqlPurchaseOrderRepository(db);
        var locationRepo = new SqlLocationRepository(db);
        var linkRepo = new SqlBackorderLinkRepository(db);
        var soRepo = new SqlSaleOrderRepository(db);

        var receivingService = new ReceivingService(
            receivingRepo,
            putAwayRepo,
            poRepo,
            locationRepo,
            uowMock.Object,
            currentUserMock.Object,
            mapper,
            null,
            linkRepo,
            soRepo);

        // Act
        var result = await receivingService.ConfirmAsync(receiving.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ReceivingStatus.Confirmed, result.Status);

        var tasks = await db.PutAwayTasks.Where(t => t.ReceivingDetailId == rDetail.Id).ToListAsync();
        Assert.Equal(2, tasks.Count);

        var crossDockTask = tasks.FirstOrDefault(t => t.IsCrossDock);
        Assert.NotNull(crossDockTask);
        Assert.Equal(4, crossDockTask.Quantity);
        Assert.Equal(crossDockLoc.Id, crossDockTask.ToLocationId);
        Assert.Contains("CrossDock giao ngay cho đơn SO: SO-SPLIT-01", crossDockTask.Notes);

        var storageTask = tasks.FirstOrDefault(t => !t.IsCrossDock);
        Assert.NotNull(storageTask);
        Assert.Equal(6, storageTask.Quantity);

        var updatedLink = await db.BackorderLinks.FirstAsync(l => l.Id == link.Id);
        Assert.Equal(4, updatedLink.FulfilledQuantity);
        Assert.True(updatedLink.IsFulfilled);

        var updatedSo = await db.SaleOrders.Include(s => s.SaleOrderDetails).FirstAsync(s => s.Id == so.Id);
        Assert.Equal(4, updatedSo.SaleOrderDetails.First().AllocatedQty);
        Assert.Equal(SaleOrderStatus.Allocated, updatedSo.Status);
    }

    [Fact]
    public async Task ReceivingConfirm_ShouldHandlePartialReceiptsAccurately()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId);
        var mapper = CreateMapper();

        var warehouse = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Code = "WH-PARTIAL", Name = "Kho Partial" };
        var crossDockLoc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = warehouse.Id, Code = "XD-01", LocationType = LocationType.CrossDock, MaxQuantity = 1000 };
        var product = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-PARTIAL", Name = "Sản phẩm đợt" };

        var po = new PurchaseOrder { Id = Guid.NewGuid(), TenantId = tenantId, PoNumber = "PO-PARTIAL-01", WarehouseId = warehouse.Id, Status = PurchaseOrderStatus.Approved };
        var poDetail = new PurchaseOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderId = po.Id, ProductId = product.Id, OrderedQuantity = 10 };
        po.PurchaseOrderDetails.Add(poDetail);

        var so = new SaleOrder { Id = Guid.NewGuid(), TenantId = tenantId, OrderNo = "SO-PARTIAL-01", WarehouseId = warehouse.Id, Status = SaleOrderStatus.Backorder, IsPreOrder = true };
        var soDetail = new SaleOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, SaleOrderId = so.Id, ProductId = product.Id, Quantity = 5, AllocatedQty = 0 };
        so.SaleOrderDetails.Add(soDetail);

        var link = new BackorderLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SaleOrderDetailId = soDetail.Id,
            PurchaseOrderDetailId = poDetail.Id,
            Quantity = 5,
            FulfilledQuantity = 0,
            IsFulfilled = false
        };

        db.Warehouses.Add(warehouse);
        db.Locations.Add(crossDockLoc);
        db.Products.Add(product);
        db.PurchaseOrders.Add(po);
        db.SaleOrders.Add(so);
        db.BackorderLinks.Add(link);
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns(true);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(() => db.SaveChangesAsync());
        uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        var receivingRepo = new SqlReceivingRepository(db);
        var putAwayRepo = new SqlPutAwayTaskRepository(db);
        var poRepo = new SqlPurchaseOrderRepository(db);
        var locationRepo = new SqlLocationRepository(db);
        var linkRepo = new SqlBackorderLinkRepository(db);
        var soRepo = new SqlSaleOrderRepository(db);

        var receivingService = new ReceivingService(
            receivingRepo,
            putAwayRepo,
            poRepo,
            locationRepo,
            uowMock.Object,
            currentUserMock.Object,
            mapper,
            null,
            linkRepo,
            soRepo);

        // Batch 1: receive 3
        var rc1 = new Receiving { Id = Guid.NewGuid(), TenantId = tenantId, ReceivingNo = "RC-1", PurchaseOrderId = po.Id, Status = ReceivingStatus.Draft };
        var rDetail1 = new ReceivingDetail { Id = Guid.NewGuid(), TenantId = tenantId, ReceivingId = rc1.Id, ProductId = product.Id, ExpectedQuantity = 10, ActualQuantity = 3, Condition = ProductCondition.Ok };
        rc1.ReceivingDetails.Add(rDetail1);
        db.Receivings.Add(rc1);
        await db.SaveChangesAsync();

        await receivingService.ConfirmAsync(rc1.Id);

        var linkAfterBatch1 = await db.BackorderLinks.FirstAsync(l => l.Id == link.Id);
        Assert.Equal(3, linkAfterBatch1.FulfilledQuantity);
        Assert.False(linkAfterBatch1.IsFulfilled);

        var soAfterBatch1 = await db.SaleOrders.FirstAsync(s => s.Id == so.Id);
        Assert.Equal(SaleOrderStatus.Backorder, soAfterBatch1.Status);

        // Batch 2: receive 2
        var rc2 = new Receiving { Id = Guid.NewGuid(), TenantId = tenantId, ReceivingNo = "RC-2", PurchaseOrderId = po.Id, Status = ReceivingStatus.Draft };
        var rDetail2 = new ReceivingDetail { Id = Guid.NewGuid(), TenantId = tenantId, ReceivingId = rc2.Id, ProductId = product.Id, ExpectedQuantity = 7, ActualQuantity = 2, Condition = ProductCondition.Ok };
        rc2.ReceivingDetails.Add(rDetail2);
        db.Receivings.Add(rc2);
        await db.SaveChangesAsync();

        await receivingService.ConfirmAsync(rc2.Id);

        var linkAfterBatch2 = await db.BackorderLinks.FirstAsync(l => l.Id == link.Id);
        Assert.Equal(5, linkAfterBatch2.FulfilledQuantity);
        Assert.True(linkAfterBatch2.IsFulfilled);

        var soAfterBatch2 = await db.SaleOrders.FirstAsync(s => s.Id == so.Id);
        Assert.Equal(SaleOrderStatus.Allocated, soAfterBatch2.Status);
    }

    [Fact]
    public async Task PutAwayComplete_CrossDock_ShouldPackSaleOrder()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId);
        var mapper = CreateMapper();

        var warehouse = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Code = "WH-PACK", Name = "Kho Pack" };
        var crossDockLoc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = warehouse.Id, Code = "XD-01", LocationType = LocationType.CrossDock, MaxQuantity = 1000 };
        var product = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-PACK", Name = "Sản phẩm đóng gói" };

        var so = new SaleOrder { Id = Guid.NewGuid(), TenantId = tenantId, OrderNo = "SO-PACK-01", WarehouseId = warehouse.Id, Status = SaleOrderStatus.Allocated };
        var soDetail = new SaleOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, SaleOrderId = so.Id, ProductId = product.Id, Quantity = 5, AllocatedQty = 5 };
        so.SaleOrderDetails.Add(soDetail);

        db.Warehouses.Add(warehouse);
        db.Locations.Add(crossDockLoc);
        db.Products.Add(product);
        db.SaleOrders.Add(so);

        var receiving = new Receiving { Id = Guid.NewGuid(), TenantId = tenantId, ReceivingNo = "RC-P-01", Status = ReceivingStatus.Confirmed };
        var receivingDetail = new ReceivingDetail { Id = Guid.NewGuid(), TenantId = tenantId, ReceivingId = receiving.Id, ProductId = product.Id, ActualQuantity = 5, Condition = ProductCondition.Ok };
        receiving.ReceivingDetails.Add(receivingDetail);
        db.Receivings.Add(receiving);

        var task = new PutAwayTask
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReceivingDetailId = receivingDetail.Id,
            ReceivingDetail = receivingDetail,
            ProductId = product.Id,
            Quantity = 5,
            ToLocationId = crossDockLoc.Id,
            Status = PutAwayTaskStatus.InProgress,
            IsCrossDock = true,
            SaleOrderId = so.Id,
            Notes = "⚡ CrossDock giao ngay cho đơn SO: SO-PACK-01"
        };
        db.PutAwayTasks.Add(task);
        await db.SaveChangesAsync();

        var currentUserId = Guid.NewGuid();
        task.AssignToId = currentUserId;
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(currentUserId);
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns(true);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(() => db.SaveChangesAsync());
        uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        var putAwayRepo = new SqlPutAwayTaskRepository(db);
        var receivingRepo = new SqlReceivingRepository(db);
        var poRepo = new SqlPurchaseOrderRepository(db);
        var stockRepo = new SqlStockRepository(db);
        var movementRepo = new SqlStockMovementRepository(db);
        var locationRepo = new SqlLocationRepository(db);
        var userManagerMock = new Mock<UserManager<User>>(Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var soRepo = new SqlSaleOrderRepository(db);

        var putAwayService = new PutAwayService(
            putAwayRepo,
            receivingRepo,
            poRepo,
            stockRepo,
            movementRepo,
            locationRepo,
            userManagerMock.Object,
            uowMock.Object,
            currentUserMock.Object,
            mapper,
            soRepo);

        // Act
        var completed = await putAwayService.CompleteAsync(task.Id);

        // Assert
        Assert.NotNull(completed);
        Assert.Equal(PutAwayTaskStatus.Completed, completed.Status);

        var updatedSo = await db.SaleOrders.FirstAsync(s => s.Id == so.Id);
        Assert.Equal(SaleOrderStatus.Packed, updatedSo.Status);
        Assert.Equal(currentUserId, updatedSo.PackedById);
    }

    [Fact]
    public async Task StockRepository_ShouldExcludeCrossDockFromAvailableInventory()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId);

        var warehouse = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Code = "WH-ISO", Name = "Kho Cô Lập" };
        var storageLoc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = warehouse.Id, Code = "ST-01", LocationType = LocationType.Storage, MaxQuantity = 1000 };
        var crossDockLoc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = warehouse.Id, Code = "XD-01", LocationType = LocationType.CrossDock, MaxQuantity = 1000 };
        var product = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-ISO", Name = "Hàng cô lập" };

        db.Warehouses.Add(warehouse);
        db.Locations.AddRange(storageLoc, crossDockLoc);
        db.Products.Add(product);

        // Stock in storage: 10
        db.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = product.Id,
            LocationId = storageLoc.Id,
            OnhandQty = 10,
            ReservedQty = 0
        });

        // Stock in CrossDock: 5
        db.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = product.Id,
            LocationId = crossDockLoc.Id,
            OnhandQty = 5,
            ReservedQty = 0
        });

        await db.SaveChangesAsync();

        var stockRepo = new SqlStockRepository(db);

        // Act
        var availableStocks = await stockRepo.GetAvailableByProductAndWarehouseAsync(product.Id, warehouse.Id);

        // Assert: only storage stock (10) is returned, CrossDock stock (5) is excluded
        Assert.Single(availableStocks);
        Assert.Equal(storageLoc.Id, availableStocks[0].LocationId);
        Assert.Equal(10, availableStocks[0].OnhandQty);
    }

    [Fact]
    public async Task CancelSaleOrder_ShouldDeleteAssociatedBackorderLinks()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId);
        var mapper = CreateMapper();

        var product = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-CANCEL", Name = "Hàng hủy" };
        var po = new PurchaseOrder { Id = Guid.NewGuid(), TenantId = tenantId, PoNumber = "PO-CAN-01", Status = PurchaseOrderStatus.Approved };
        var poDetail = new PurchaseOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderId = po.Id, ProductId = product.Id, OrderedQuantity = 10 };
        po.PurchaseOrderDetails.Add(poDetail);

        var so = new SaleOrder { Id = Guid.NewGuid(), TenantId = tenantId, OrderNo = "SO-CAN-01", Status = SaleOrderStatus.Backorder, IsPreOrder = true };
        var soDetail = new SaleOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, SaleOrderId = so.Id, ProductId = product.Id, Quantity = 3 };
        so.SaleOrderDetails.Add(soDetail);

        var link = new BackorderLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SaleOrderDetailId = soDetail.Id,
            PurchaseOrderDetailId = poDetail.Id,
            Quantity = 3
        };

        db.Products.Add(product);
        db.PurchaseOrders.Add(po);
        db.SaleOrders.Add(so);
        db.BackorderLinks.Add(link);
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns(true);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(() => db.SaveChangesAsync());

        var soRepo = new SqlSaleOrderRepository(db);
        var productRepo = new SqlProductRepository(db);
        var customerRepo = new SqlCustomerRepository(db);
        var pickingMock = new Mock<IPickingService>();
        var linkRepo = new SqlBackorderLinkRepository(db);

        var soService = new SaleOrderService(
            soRepo,
            productRepo,
            customerRepo,
            pickingMock.Object,
            mapper,
            uowMock.Object,
            currentUserMock.Object,
            linkRepo);

        // Act
        var cancelled = await soService.CancelAsync(so.Id, "Khách đổi ý");

        // Assert
        Assert.NotNull(cancelled);
        Assert.Equal(SaleOrderStatus.Cancelled, cancelled.Status);

        var linksAfterCancel = await db.BackorderLinks.Where(b => b.SaleOrderDetailId == soDetail.Id).ToListAsync();
        Assert.Empty(linksAfterCancel);
    }

    [Fact]
    public async Task CreateBackorderLink_ShouldThrow_OnProductAndWarehouseMismatch_AndPoCapacityExceeded()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId);
        var mapper = CreateMapper();

        var wh1 = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Code = "WH-1", Name = "Kho 1" };
        var wh2 = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Code = "WH-2", Name = "Kho 2" };
        var prodA = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-A", Name = "Sản phẩm A" };
        var prodB = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-B", Name = "Sản phẩm B" };

        var po = new PurchaseOrder { Id = Guid.NewGuid(), TenantId = tenantId, PoNumber = "PO-MISMATCH", WarehouseId = wh1.Id, Status = PurchaseOrderStatus.Approved };
        var poDetailA = new PurchaseOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderId = po.Id, ProductId = prodA.Id, OrderedQuantity = 5, ReceivedQuantity = 0 };
        var poDetailB = new PurchaseOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderId = po.Id, ProductId = prodB.Id, OrderedQuantity = 5, ReceivedQuantity = 0 };
        po.PurchaseOrderDetails.Add(poDetailA);
        po.PurchaseOrderDetails.Add(poDetailB);

        var soWh1 = new SaleOrder { Id = Guid.NewGuid(), TenantId = tenantId, OrderNo = "SO-WH1", WarehouseId = wh1.Id, Status = SaleOrderStatus.Backorder, IsPreOrder = true };
        var soWh1DetailA = new SaleOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, SaleOrderId = soWh1.Id, ProductId = prodA.Id, Quantity = 5 };
        soWh1.SaleOrderDetails.Add(soWh1DetailA);

        var soWh2 = new SaleOrder { Id = Guid.NewGuid(), TenantId = tenantId, OrderNo = "SO-WH2", WarehouseId = wh2.Id, Status = SaleOrderStatus.Backorder, IsPreOrder = true };
        var soWh2DetailA = new SaleOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, SaleOrderId = soWh2.Id, ProductId = prodA.Id, Quantity = 5 };
        soWh2.SaleOrderDetails.Add(soWh2DetailA);

        db.Warehouses.AddRange(wh1, wh2);
        db.Products.AddRange(prodA, prodB);
        db.PurchaseOrders.Add(po);
        db.SaleOrders.AddRange(soWh1, soWh2);
        await db.SaveChangesAsync();

        var linkRepo = new SqlBackorderLinkRepository(db);
        var soRepo = new SqlSaleOrderRepository(db);
        var poRepo = new SqlPurchaseOrderRepository(db);
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(() => db.SaveChangesAsync());

        var service = new BackorderLinkService(linkRepo, soRepo, poRepo, uowMock.Object, mapper);

        // 1. Product mismatch
        var exProduct = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateBackorderLinkDto
            {
                SaleOrderDetailId = soWh1DetailA.Id,
                PurchaseOrderDetailId = poDetailB.Id,
                Quantity = 2
            }));
        Assert.Contains("Sản phẩm của dòng đơn bán và đơn mua không khớp nhau", exProduct.Message);

        // 2. Warehouse mismatch
        var exWarehouse = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateBackorderLinkDto
            {
                SaleOrderDetailId = soWh2DetailA.Id,
                PurchaseOrderDetailId = poDetailA.Id,
                Quantity = 2
            }));
        Assert.Contains("Kho hàng của đơn bán và đơn mua không khớp nhau", exWarehouse.Message);

        // 3. PO remaining capacity exceeded
        poDetailA.ReceivedQuantity = 3; // ordered 5, received 3 -> remaining capacity is 2
        await db.SaveChangesAsync();

        var exPoCapacity = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateBackorderLinkDto
            {
                SaleOrderDetailId = soWh1DetailA.Id,
                PurchaseOrderDetailId = poDetailA.Id,
                Quantity = 3
            }));
        Assert.Contains("vượt quá số lượng khả dụng còn lại của dòng đơn mua", exPoCapacity.Message);
    }

    [Fact]
    public async Task DeleteBackorderLink_ShouldSucceedWhenUnfulfilled_AndThrowWhenFulfilled()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId);
        var mapper = CreateMapper();

        var product = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-DEL", Name = "Sản phẩm Delete" };
        var po = new PurchaseOrder { Id = Guid.NewGuid(), TenantId = tenantId, PoNumber = "PO-DEL", Status = PurchaseOrderStatus.Approved };
        var poDetail = new PurchaseOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderId = po.Id, ProductId = product.Id, OrderedQuantity = 10 };
        po.PurchaseOrderDetails.Add(poDetail);

        var so = new SaleOrder { Id = Guid.NewGuid(), TenantId = tenantId, OrderNo = "SO-DEL", Status = SaleOrderStatus.Backorder, IsPreOrder = true };
        var soDetail = new SaleOrderDetail { Id = Guid.NewGuid(), TenantId = tenantId, SaleOrderId = so.Id, ProductId = product.Id, Quantity = 5 };
        so.SaleOrderDetails.Add(soDetail);

        var unfulfilledLink = new BackorderLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SaleOrderDetailId = soDetail.Id,
            PurchaseOrderDetailId = poDetail.Id,
            Quantity = 3,
            FulfilledQuantity = 0,
            IsFulfilled = false
        };

        var fulfilledLink = new BackorderLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SaleOrderDetailId = soDetail.Id,
            PurchaseOrderDetailId = poDetail.Id,
            Quantity = 2,
            FulfilledQuantity = 1,
            IsFulfilled = false
        };

        db.Products.Add(product);
        db.PurchaseOrders.Add(po);
        db.SaleOrders.Add(so);
        db.BackorderLinks.AddRange(unfulfilledLink, fulfilledLink);
        await db.SaveChangesAsync();

        var linkRepo = new SqlBackorderLinkRepository(db);
        var soRepo = new SqlSaleOrderRepository(db);
        var poRepo = new SqlPurchaseOrderRepository(db);
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(() => db.SaveChangesAsync());

        var service = new BackorderLinkService(linkRepo, soRepo, poRepo, uowMock.Object, mapper);

        // 1. Unfulfilled link delete succeeds
        var deleted = await service.DeleteAsync(unfulfilledLink.Id);
        Assert.True(deleted);

        // 2. Fulfilled or partially fulfilled link delete throws
        var exFulfilled = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync(fulfilledLink.Id));
        Assert.Contains("Không thể hủy liên kết đã được nhận một phần hoặc hoàn tất", exFulfilled.Message);
    }
}
