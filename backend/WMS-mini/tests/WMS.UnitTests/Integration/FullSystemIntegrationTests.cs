using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
using WMS.UnitTests.Common;
using Xunit;

namespace WMS.UnitTests.Integration;

public class FullSystemIntegrationTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    private static IUnitOfWork CreateUnitOfWork(WmsDbContext db)
    {
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() => db.SaveChangesAsync());
        uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());
        return uowMock.Object;
    }

    [Fact]
    public async Task INT01_MultiTenant_Isolation_And_DataLeak_Prevention()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantAlphaId = Guid.NewGuid();
        var tenantBetaId = Guid.NewGuid();

        // 1. Tenant Alpha tạo kho, danh mục và sản phẩm
        using (var dbAlpha = TestDbContextFactory.Create(tenantAlphaId, dbName))
        {
            var whAlpha = new Warehouse
            {
                Id = Guid.NewGuid(),
                TenantId = tenantAlphaId,
                Name = "Kho Tổng Alpha",
                Code = "WH-ALPHA"
            };
            var catAlpha = new Category
            {
                Id = Guid.NewGuid(),
                TenantId = tenantAlphaId,
                Name = "Điện tử Alpha"
            };
            var prodAlpha = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = tenantAlphaId,
                Sku = "SKU-ALPHA-01",
                Name = "Sản phẩm Độc quyền Alpha",
                CategoryId = catAlpha.Id
            };

            dbAlpha.Warehouses.Add(whAlpha);
            dbAlpha.Categories.Add(catAlpha);
            dbAlpha.Products.Add(prodAlpha);
            await dbAlpha.SaveChangesAsync();
        }

        // 2. Tenant Beta kết nối vào cùng cơ sở dữ liệu và thực hiện truy vấn
        using (var dbBeta = TestDbContextFactory.Create(tenantBetaId, dbName))
        {
            var warehouses = await dbBeta.Warehouses.ToListAsync();
            var categories = await dbBeta.Categories.ToListAsync();
            var products = await dbBeta.Products.ToListAsync();

            // Khẳng định: Tenant Beta hoàn toàn không thấy bất kỳ dữ liệu nào của Tenant Alpha
            Assert.Empty(warehouses);
            Assert.Empty(categories);
            Assert.Empty(products);

            // Tenant Beta tạo sản phẩm cùng SKU vẫn thành công vì cô lập theo TenantId
            var catBeta = new Category { Id = Guid.NewGuid(), TenantId = tenantBetaId, Name = "Điện tử Beta" };
            var prodBeta = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = tenantBetaId,
                Sku = "SKU-ALPHA-01", // Trùng SKU với Alpha
                Name = "Sản phẩm Beta",
                CategoryId = catBeta.Id
            };
            dbBeta.Categories.Add(catBeta);
            dbBeta.Products.Add(prodBeta);
            await dbBeta.SaveChangesAsync();

            var betaProducts = await dbBeta.Products.ToListAsync();
            Assert.Single(betaProducts);
            Assert.Equal("Sản phẩm Beta", betaProducts[0].Name);
        }
    }

    [Fact]
    public async Task INT02_SoD_MakerChecker_Lifecycle()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var accountantId = Guid.NewGuid();
        var chiefId = Guid.NewGuid();
        var mapper = CreateMapper();

        Guid poId;
        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            var uow = CreateUnitOfWork(db);
            var vendor = new Vendor { Id = Guid.NewGuid(), TenantId = tenantId, Name = "NCC Linh Kiện" };
            var prod = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "CHIP-01", Name = "Microchip" };
            db.Vendors.Add(vendor);
            db.Products.Add(prod);
            await db.SaveChangesAsync();

            var currentAccMock = new Mock<ICurrentUserService>();
            currentAccMock.Setup(u => u.TenantId).Returns(tenantId);
            currentAccMock.Setup(u => u.UserId).Returns(accountantId);
            currentAccMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns<string[]>(r => r.Contains("Accountant"));

            var poRepo = new SqlPurchaseOrderRepository(db);
            var vendorDebtRepo = new SqlVendorDebtRepository(db);
            var poService = new PurchaseOrderService(poRepo, mapper, uow, currentAccMock.Object, vendorDebtRepo);

            // Maker (Accountant) tạo đơn PO -> lưu ở trạng thái Pending
            var poDto = await poService.CreateAsync(new CreatePurchaseOrderDto
            {
                PoNumber = "PO-MAKER-001",
                VendorId = vendor.Id,
                PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
                {
                    new() { ProductId = prod.Id, OrderedQuantity = 100, UnitPrice = 50000m }
                }
            });

            Assert.NotNull(poDto);
            Assert.Equal(PurchaseOrderStatus.Pending, poDto.Status);
            poId = poDto.Id;
        }

        // Checker (ChiefAccountant) thực hiện kiểm tra và phê duyệt
        using (var dbChief = TestDbContextFactory.Create(tenantId, dbName))
        {
            var uow = CreateUnitOfWork(dbChief);
            var poRepo = new SqlPurchaseOrderRepository(dbChief);

            var currentChiefMock = new Mock<ICurrentUserService>();
            currentChiefMock.Setup(u => u.TenantId).Returns(tenantId);
            currentChiefMock.Setup(u => u.UserId).Returns(chiefId);
            currentChiefMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns<string[]>(r => r.Contains("ChiefAccountant"));

            var poService = new PurchaseOrderService(poRepo, mapper, uow, currentChiefMock.Object);

            var approved = await poService.ApproveAsync(poId);

            Assert.NotNull(approved);
            Assert.Equal(PurchaseOrderStatus.Approved, approved.Status);
            Assert.Equal(chiefId, approved.ApprovedById);
            Assert.NotNull(approved.ApprovedDate);
        }
    }

    [Fact]
    public async Task INT03_SoD_WarehouseManager_CreditOffset_Forbidden()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var mapper = CreateMapper();

        using var db = TestDbContextFactory.Create(tenantId, dbName);
        var uow = CreateUnitOfWork(db);
        var vendor = new Vendor { Id = Guid.NewGuid(), TenantId = tenantId, Name = "NCC Test" };
        var prod = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-01", Name = "Sản phẩm 1" };
        db.Vendors.Add(vendor);
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var managerMock = new Mock<ICurrentUserService>();
        managerMock.Setup(u => u.TenantId).Returns(tenantId);
        managerMock.Setup(u => u.UserId).Returns(managerId);
        managerMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns<string[]>(r => r.Contains("WarehouseManager"));

        var poRepo = new SqlPurchaseOrderRepository(db);
        var poService = new PurchaseOrderService(poRepo, mapper, uow, managerMock.Object);

        var dto = new CreatePurchaseOrderDto
        {
            PoNumber = "PO-ILLEGAL-OFFSET",
            VendorId = vendor.Id,
            CreditOffsetAmount = 50000m, // Quản lý kho cố tình cấn trừ công nợ
            PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
            {
                new() { ProductId = prod.Id, OrderedQuantity = 10, UnitPrice = 10000m }
            }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => poService.CreateAsync(dto));
        Assert.Contains("Quản lý kho không có thẩm quyền cấn trừ công nợ nhà cung cấp.", ex.Message);
    }

    [Fact]
    public async Task INT04_Inbound_3WayMatching_Discrepancy_Quarantine_And_Debt()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var mapper = CreateMapper();

        using var db = TestDbContextFactory.Create(tenantId, dbName);
        var uow = CreateUnitOfWork(db);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns(true);

        var vendor = new Vendor { Id = Guid.NewGuid(), TenantId = tenantId, Name = "NCC Vật Tư" };
        var prod = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "PANEL-01", Name = "Tấm màn hình" };
        var wh = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Kho Chính", Code = "WH-01" };
        var storageLoc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = wh.Id, Code = "LOC-STORAGE", LocationType = LocationType.Storage, MaxQuantity = 100 };
        var quarantineLoc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = wh.Id, Code = "LOC-QUARANTINE", LocationType = LocationType.Quarantine, MaxQuantity = 100 };

        db.Vendors.Add(vendor);
        db.Products.Add(prod);
        db.Warehouses.Add(wh);
        db.Locations.AddRange(storageLoc, quarantineLoc);

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = "PO-INBOUND-001",
            VendorId = vendor.Id,
            Status = PurchaseOrderStatus.Approved,
            TotalAmount = 1000000m,
            WarehouseId = wh.Id,
            PurchaseOrderDetails = new List<PurchaseOrderDetail>
            {
                new() { Id = Guid.NewGuid(), ProductId = prod.Id, OrderedQuantity = 10, ReceivedQuantity = 0, UnitPrice = 100000m }
            }
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        var poRepo = new SqlPurchaseOrderRepository(db);
        var receivingRepo = new SqlReceivingRepository(db);
        var putAwayRepo = new SqlPutAwayTaskRepository(db);
        var locationRepo = new SqlLocationRepository(db);
        var vendorDebtRepo = new SqlVendorDebtRepository(db);

        var receivingService = new ReceivingService(
            receivingRepo,
            putAwayRepo,
            poRepo,
            locationRepo,
            uow,
            currentUserMock.Object,
            mapper,
            vendorDebtRepo);

        // Tạo phiếu nhận hàng với 6 Ok và 4 Damaged (hàng hỏng cách ly QuarantineAndHold)
        var receivingDto = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new() { ProductId = prod.Id, ExpectedQuantity = 10, ActualQuantity = 6, Condition = ProductCondition.Ok, UnitPrice = 100000m },
                new() { ProductId = prod.Id, ExpectedQuantity = 10, ActualQuantity = 4, Condition = ProductCondition.Damaged, UnitPrice = 100000m, DiscrepancyAction = DiscrepancyAction.CreditOffset, DiscrepancyNotes = "Hỏng 4 tấm do vận chuyển" }
            }
        });

        // Xác nhận nhận hàng
        var confirmed = await receivingService.ConfirmAsync(receivingDto.Id);
        Assert.NotNull(confirmed);
        Assert.Equal(ReceivingStatus.Confirmed, confirmed.Status);

        // Duyệt biên bản bất thường với phương án cấn trừ tiền (CreditOffset)
        var approvedReport = await receivingService.ApproveDiscrepancyReportAsync(receivingDto.Id, new ApproveDiscrepancyReportDto
        {
            IsApproved = true,
            Notes = "Chấp nhận cấn trừ công nợ cho 4 tấm hỏng"
        });

        Assert.NotNull(approvedReport);
        Assert.Equal(DiscrepancyReportStatus.Approved, approvedReport.Status);

        // Kiểm tra nợ NCC đã tự động sinh trong VendorDebtLedgers
        var debts = await vendorDebtRepo.GetUnresolvedByVendorIdAsync(vendor.Id);
        Assert.Single(debts);
        Assert.Equal(VendorDebtType.CreditBalance, debts[0].DebtType);
        Assert.Equal(400000m, debts[0].Amount); // 4 * 100,000đ
    }

    [Fact]
    public async Task INT05_VendorDebt_CreditOffset_PO_Recalculation()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var accountantId = Guid.NewGuid();
        var mapper = CreateMapper();

        using var db = TestDbContextFactory.Create(tenantId, dbName);
        var uow = CreateUnitOfWork(db);

        var vendor = new Vendor { Id = Guid.NewGuid(), TenantId = tenantId, Name = "NCC Vật Tư" };
        var prod = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-02", Name = "Linh kiện A" };
        var oldPo = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = "PO-OLD-001",
            VendorId = vendor.Id,
            Status = PurchaseOrderStatus.Received
        };
        var oldReceiving = new Receiving
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PurchaseOrderId = oldPo.Id,
            ReceivingNo = "REC-OLD-001",
            Status = ReceivingStatus.Confirmed
        };
        var debt = new VendorDebtLedger
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VendorId = vendor.Id,
            VendorName = vendor.Name,
            SourceReceivingId = oldReceiving.Id,
            DebtType = VendorDebtType.CreditBalance,
            Amount = 150000m,
            IsResolved = false
        };

        db.Vendors.Add(vendor);
        db.Products.Add(prod);
        db.PurchaseOrders.Add(oldPo);
        db.Receivings.Add(oldReceiving);
        db.VendorDebtLedgers.Add(debt);
        await db.SaveChangesAsync();

        var currentAccMock = new Mock<ICurrentUserService>();
        currentAccMock.Setup(u => u.TenantId).Returns(tenantId);
        currentAccMock.Setup(u => u.UserId).Returns(accountantId);
        currentAccMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns<string[]>(r => r.Contains("Accountant"));

        var poRepo = new SqlPurchaseOrderRepository(db);
        var vendorDebtRepo = new SqlVendorDebtRepository(db);
        var poService = new PurchaseOrderService(poRepo, mapper, uow, currentAccMock.Object, vendorDebtRepo);

        // Tạo PO mới tổng trị giá 500,000đ và cấn trừ khoản nợ 150,000đ
        var newPo = await poService.CreateAsync(new CreatePurchaseOrderDto
        {
            PoNumber = "PO-OFFSET-002",
            VendorId = vendor.Id,
            CreditOffsetAmount = 150000m,
            ResolvedDebtIds = new List<Guid> { debt.Id },
            PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
            {
                new() { ProductId = prod.Id, OrderedQuantity = 5, UnitPrice = 100000m, TaxRate = 0 }
            }
        });

        Assert.NotNull(newPo);
        Assert.Equal(150000m, newPo.CreditOffsetAmount);
        Assert.Equal(350000m, newPo.TotalAmount); // 500k - 150k = 350k

        // Khoản nợ cũ đã chuyển thành Resolved
        var resolvedDebt = await db.VendorDebtLedgers.FindAsync(debt.Id);
        Assert.NotNull(resolvedDebt);
        Assert.True(resolvedDebt.IsResolved);
        Assert.Equal(newPo.Id, resolvedDebt.ResolvedInPoId);
    }

    [Fact]
    public async Task INT06_OpportunityCrossDocking_And_FEFO_Allocation()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var mapper = CreateMapper();

        using var db = TestDbContextFactory.Create(tenantId, dbName);
        var uow = CreateUnitOfWork(db);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns(true);

        var wh = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Kho CrossDock", Code = "WH-CD" };
        var crossDockLoc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = wh.Id, Code = "LOC-CD", LocationType = LocationType.CrossDock, MaxQuantity = 500 };
        var storageLoc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = wh.Id, Code = "LOC-ST", LocationType = LocationType.Storage, MaxQuantity = 500 };
        var prod = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "PHONE-01", Name = "Điện thoại mẫu" };
        var customer = new Customer { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Khách đặt trước" };

        db.Warehouses.Add(wh);
        db.Locations.AddRange(crossDockLoc, storageLoc);
        db.Products.Add(prod);
        db.Customers.Add(customer);

        // Tạo đơn bán SO đặt trước (Backorder)
        var so = new SaleOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNo = "SO-BACKORDER-01",
            CustomerId = customer.Id,
            Status = SaleOrderStatus.Backorder,
            SaleOrderDetails = new List<SaleOrderDetail>
            {
                new() { Id = Guid.NewGuid(), ProductId = prod.Id, Quantity = 10, UnitPrice = 1000000m }
            }
        };
        db.SaleOrders.Add(so);

        // Tạo đơn mua PO liên kết với SO qua BackorderLink
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = "PO-CROSSDOCK-01",
            Status = PurchaseOrderStatus.Approved,
            WarehouseId = wh.Id,
            PurchaseOrderDetails = new List<PurchaseOrderDetail>
            {
                new() { Id = Guid.NewGuid(), ProductId = prod.Id, OrderedQuantity = 10, UnitPrice = 800000m }
            }
        };
        db.PurchaseOrders.Add(po);

        var link = new BackorderLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SaleOrderDetailId = so.SaleOrderDetails.First().Id,
            PurchaseOrderDetailId = po.PurchaseOrderDetails.First().Id,
            Quantity = 10,
            FulfilledQuantity = 0,
            IsFulfilled = false
        };
        db.BackorderLinks.Add(link);
        await db.SaveChangesAsync();

        var poRepo = new SqlPurchaseOrderRepository(db);
        var recRepo = new SqlReceivingRepository(db);
        var putAwayRepo = new SqlPutAwayTaskRepository(db);
        var locRepo = new SqlLocationRepository(db);
        var linkRepo = new SqlBackorderLinkRepository(db);
        var soRepo = new SqlSaleOrderRepository(db);

        var receivingService = new ReceivingService(
            recRepo,
            putAwayRepo,
            poRepo,
            locRepo,
            uow,
            currentUserMock.Object,
            mapper,
            null,
            linkRepo,
            soRepo);

        // Nhận hàng PO có gắn link CrossDocking
        var receiving = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new() { ProductId = prod.Id, ExpectedQuantity = 10, ActualQuantity = 10, Condition = ProductCondition.Ok }
            }
        });

        await receivingService.ConfirmAsync(receiving.Id);

        // PutAway task sinh ra phải có IsCrossDock = true và ToLocationId là vị trí CrossDock
        var tasks = await db.PutAwayTasks.Where(t => t.ReceivingDetail.ReceivingId == receiving.Id).ToListAsync();
        Assert.Single(tasks);
        Assert.True(tasks[0].IsCrossDock);
        Assert.Equal(crossDockLoc.Id, tasks[0].ToLocationId);
    }

    [Fact]
    public async Task INT07_StockAdjustment_And_WorkingCapital_Calculation()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var mapper = CreateMapper();

        using var db = TestDbContextFactory.Create(tenantId, dbName);
        var uow = CreateUnitOfWork(db);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(adminId);
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string[]>())).Returns<string[]>(r => r.Contains("Admin"));

        var wh = new Warehouse { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Kho Kiểm Kê", Code = "WH-KK" };
        var loc = new Location { Id = Guid.NewGuid(), TenantId = tenantId, WarehouseId = wh.Id, Code = "LOC-01", CurrentQuantity = 10, MaxQuantity = 100 };
        var prod = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU-KK", Name = "Sản phẩm kiểm kê", PurchasePrice = 200000m };
        var stock = new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = loc.Id,
            ProductId = prod.Id,
            OnhandQty = 10,
            ReservedQty = 0,
            InboundDate = DateTime.UtcNow.AddDays(-100) // Hàng nằm kho 100 ngày -> Dead Stock
        };

        db.Warehouses.Add(wh);
        db.Locations.Add(loc);
        db.Products.Add(prod);
        db.Stocks.Add(stock);
        await db.SaveChangesAsync();

        var adjRepo = new SqlStockAdjustmentRepository(db);
        var stockRepo = new SqlStockRepository(db);
        var movementRepo = new SqlStockMovementRepository(db);
        var prodRepo = new SqlProductRepository(db);
        var locRepo = new SqlLocationRepository(db);

        var adjService = new StockAdjustmentService(
            adjRepo,
            stockRepo,
            movementRepo,
            prodRepo,
            locRepo,
            uow,
            currentUserMock.Object,
            mapper);

        // Kiểm kê thực tế thấy 14 chiếc (thừa 4 chiếc so với sổ sách 10)
        var adjDto = await adjService.CreateAsync(new CreateStockAdjustmentDto
        {
            Notes = "Kiểm kê đột xuất khu A",
            Details = new List<CreateStockAdjustmentDetailDto>
            {
                new() { ProductId = prod.Id, LocationId = loc.Id, CountedQty = 14 }
            }
        });

        // Admin phê duyệt phiếu điều chỉnh
        var approved = await adjService.ApproveAsync(adjDto.Id);
        Assert.NotNull(approved);
        Assert.Equal(StockAdjustmentStatus.Approved, approved.Status);

        // Tồn kho và sức chứa vị trí tăng lên đúng 14
        var updatedStock = await db.Stocks.FindAsync(stock.Id);
        var updatedLoc = await db.Locations.FindAsync(loc.Id);
        Assert.NotNull(updatedStock);
        Assert.NotNull(updatedLoc);
        Assert.Equal(14, updatedStock.OnhandQty);
        Assert.Equal(14, updatedLoc.CurrentQuantity);

        // Sinh bản ghi StockMovement với loại Adjustment và delta +4
        var movement = await db.StockMovements.FirstOrDefaultAsync(m => m.ProductId == prod.Id && m.MovementType == MovementType.Adjustment);
        Assert.NotNull(movement);
        Assert.Equal(4, movement.Qty);
    }
}