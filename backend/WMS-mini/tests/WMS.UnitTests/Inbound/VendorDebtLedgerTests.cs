using AutoMapper;
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
using WMS.UnitTests.Common;
using Xunit;

namespace WMS.UnitTests.Inbound;

public class VendorDebtLedgerTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    private static (
        ReceivingService receivingService,
        PurchaseOrderService poService,
        VendorDebtService vendorDebtService,
        WmsDbContext db,
        Guid tenantId,
        Guid userId)
        CreateTestContext(string? userRole = "WarehouseManager")
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId, Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.UserName).Returns("manager_tester");
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string[]>()))
            .Returns<string[]>(roles => userRole != null && (roles.Contains(userRole) || userRole == "All"));

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() => db.SaveChangesAsync());
        uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        var receivingRepo = new SqlReceivingRepository(db);
        var putAwayRepo = new SqlPutAwayTaskRepository(db);
        var poRepo = new SqlPurchaseOrderRepository(db);
        var locationRepo = new SqlLocationRepository(db);
        var vendorDebtRepo = new SqlVendorDebtRepository(db);

        var receivingService = new ReceivingService(
            receivingRepo,
            putAwayRepo,
            poRepo,
            locationRepo,
            uowMock.Object,
            currentUserMock.Object,
            mapper,
            vendorDebtRepo);

        var poService = new PurchaseOrderService(
            poRepo,
            mapper,
            uowMock.Object,
            currentUserMock.Object,
            vendorDebtRepo);

        var vendorDebtService = new VendorDebtService(vendorDebtRepo, mapper);

        return (receivingService, poService, vendorDebtService, db, tenantId, userId);
    }

    private static async Task<(PurchaseOrder po, Product product, Vendor vendor, Receiving receiving)>
        SeedMasterDataAsync(WmsDbContext db, Guid tenantId)
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Công ty TNHH Phân phối ABC",
            TenantId = tenantId
        };
        db.Vendors.Add(vendor);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Điện tử gia dụng",
            TenantId = tenantId
        };
        db.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = "SKU-APPLE-01",
            Name = "Táo Envy New Zealand",
            CategoryId = category.Id,
            Price = 100000m,
            TenantId = tenantId
        };
        db.Products.Add(product);

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = $"PO-{Guid.NewGuid():N}"[..12],
            VendorId = vendor.Id,
            VendorName = vendor.Name,
            Status = PurchaseOrderStatus.Approved,
            TenantId = tenantId,
            TotalAmount = 2000000m,
            PurchaseOrderDetails = new List<PurchaseOrderDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Product = product,
                    OrderedQuantity = 20,
                    ReceivedQuantity = 0,
                    UnitPrice = 100000m,
                    TaxRate = 0,
                    TenantId = tenantId
                }
            }
        };
        db.PurchaseOrders.Add(po);

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Kho Tổng",
            Code = "WH-MAIN",
            TenantId = tenantId
        };
        db.Warehouses.Add(warehouse);

        var storageLoc = new Location
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouse.Id,
            Code = "LOC-STORAGE-01",
            LocationType = LocationType.Storage,
            MaxQuantity = 1000,
            TenantId = tenantId
        };
        db.Locations.Add(storageLoc);

        var receiving = new Receiving
        {
            Id = Guid.NewGuid(),
            ReceivingNo = $"REC-{Guid.NewGuid():N}"[..12],
            PurchaseOrderId = po.Id,
            PurchaseOrder = po,
            TenantId = tenantId,
            Status = ReceivingStatus.Confirmed
        };
        db.Receivings.Add(receiving);

        await db.SaveChangesAsync();
        return (po, product, vendor, receiving);
    }

    [Fact]
    public async Task ApproveDiscrepancyReport_WithBacklogDeliverLater_CreatesGoodsBacklogVendorDebt()
    {
        var (receivingService, _, vendorDebtService, db, tenantId, _) = CreateTestContext();
        var (po, product, vendor, _) = await SeedMasterDataAsync(db, tenantId);

        var receivingDto = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 20,
                    ActualQuantity = 10,
                    Condition = ProductCondition.Ok,
                    UnitPrice = 100000m,
                    DiscrepancyAction = DiscrepancyAction.BacklogDeliverLater,
                    DiscrepancyNotes = "NCC hẹn giao bù 10 quả vào đợt sau"
                }
            }
        });

        Assert.True(receivingDto.HasDiscrepancy);
        Assert.NotNull(receivingDto.DiscrepancyReport);

        var approvedReport = await receivingService.ApproveDiscrepancyReportAsync(
            receivingDto.Id,
            new ApproveDiscrepancyReportDto
            {
                IsApproved = true,
                Notes = "Đồng ý cho NCC giao bù 10 quả"
            });

        Assert.Equal(DiscrepancyReportStatus.Approved, approvedReport.Status);

        var debts = await vendorDebtService.GetUnresolvedByVendorIdAsync(vendor.Id);
        Assert.Single(debts);

        var debt = debts[0];
        Assert.Equal(VendorDebtType.GoodsBacklog, debt.DebtType);
        Assert.Equal(product.Id, debt.ProductId);
        Assert.Equal(10, debt.Quantity);
        Assert.Null(debt.Amount);
        Assert.False(debt.IsResolved);
        Assert.Equal(vendor.Id, debt.VendorId);
    }

    [Fact]
    public async Task ApproveDiscrepancyReport_WithCreditOffset_CreatesCreditBalanceVendorDebt()
    {
        var (receivingService, _, vendorDebtService, db, tenantId, _) = CreateTestContext();
        var (po, product, vendor, _) = await SeedMasterDataAsync(db, tenantId);

        var receivingDto = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 20,
                    ActualQuantity = 15,
                    Condition = ProductCondition.Ok,
                    UnitPrice = 100000m,
                    DiscrepancyAction = DiscrepancyAction.CreditOffset,
                    DiscrepancyNotes = "Thiếu 5 quả, cấn trừ công nợ"
                }
            }
        });

        await receivingService.ApproveDiscrepancyReportAsync(
            receivingDto.Id,
            new ApproveDiscrepancyReportDto
            {
                IsApproved = true,
                Notes = "Duyệt cấn trừ công nợ cho 5 quả thiếu"
            });

        var debts = await vendorDebtService.GetUnresolvedByVendorIdAsync(vendor.Id);
        Assert.Single(debts);

        var debt = debts[0];
        Assert.Equal(VendorDebtType.CreditBalance, debt.DebtType);
        Assert.Equal(product.Id, debt.ProductId);
        Assert.Equal(5, debt.Quantity);
        Assert.Equal(500000m, debt.Amount); // 5 * 100,000
        Assert.False(debt.IsResolved);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithCreditOffsetAndResolvedDebt_ReducesTotalAmountAndMarksDebtResolved()
    {
        var (_, poService, vendorDebtService, db, tenantId, _) = CreateTestContext();
        var (_, product, vendor, receiving) = await SeedMasterDataAsync(db, tenantId);

        var debt = new VendorDebtLedger
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VendorId = vendor.Id,
            VendorName = vendor.Name,
            SourceReceivingId = receiving.Id,
            DebtType = VendorDebtType.CreditBalance,
            ProductId = product.Id,
            Quantity = 5,
            Amount = 500000m,
            IsResolved = false,
            CreatedDate = DateTime.UtcNow
        };
        db.VendorDebtLedgers.Add(debt);
        await db.SaveChangesAsync();

        var newPo = await poService.CreateAsync(new CreatePurchaseOrderDto
        {
            PoNumber = "PO-2026-OFFSET-01",
            VendorId = vendor.Id,
            VendorName = vendor.Name,
            CreditOffsetAmount = 500000m,
            ResolvedDebtIds = new List<Guid> { debt.Id },
            PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    OrderedQuantity = 10,
                    UnitPrice = 100000m,
                    TaxRate = 0
                }
            }
        });

        // Raw total = 10 * 100,000 = 1,000,000
        // Offset = 500,000 -> TotalAmount = 500,000
        Assert.Equal(500000m, newPo.CreditOffsetAmount);
        Assert.Equal(500000m, newPo.TotalAmount);
        Assert.Contains(debt.Id, newPo.ResolvedDebtIds);

        var unresolvedDebts = await vendorDebtService.GetUnresolvedByVendorIdAsync(vendor.Id);
        Assert.Empty(unresolvedDebts);

        var debtEntity = await db.VendorDebtLedgers.FindAsync(debt.Id);
        Assert.NotNull(debtEntity);
        Assert.True(debtEntity.IsResolved);
        Assert.Equal(newPo.Id, debtEntity.ResolvedInPoId);
        Assert.NotNull(debtEntity.ResolvedDate);
    }

    [Fact]
    public async Task CreatePurchaseOrder_CreditOffsetExceedsRawTotal_ThrowsInvalidOperationException()
    {
        var (_, poService, _, db, tenantId, _) = CreateTestContext();
        var (_, product, vendor, _) = await SeedMasterDataAsync(db, tenantId);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            poService.CreateAsync(new CreatePurchaseOrderDto
            {
                PoNumber = "PO-2026-EXCEED-01",
                VendorId = vendor.Id,
                VendorName = vendor.Name,
                CreditOffsetAmount = 2000000m, // Exceeds raw total 1,000,000
                PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
                {
                    new()
                    {
                        ProductId = product.Id,
                        OrderedQuantity = 10,
                        UnitPrice = 100000m,
                        TaxRate = 0
                    }
                }
            }));

        Assert.Contains("không được vượt quá tổng giá trị đơn hàng", ex.Message);
    }

    [Fact]
    public async Task DeletePurchaseOrder_PendingWithResolvedDebts_RevertsDebtsToUnresolved()
    {
        var (_, poService, vendorDebtService, db, tenantId, _) = CreateTestContext();
        var (_, product, vendor, receiving) = await SeedMasterDataAsync(db, tenantId);

        var debt = new VendorDebtLedger
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VendorId = vendor.Id,
            VendorName = vendor.Name,
            SourceReceivingId = receiving.Id,
            DebtType = VendorDebtType.GoodsBacklog,
            ProductId = product.Id,
            Quantity = 10,
            IsResolved = false,
            CreatedDate = DateTime.UtcNow
        };
        db.VendorDebtLedgers.Add(debt);
        await db.SaveChangesAsync();

        var po = await poService.CreateAsync(new CreatePurchaseOrderDto
        {
            PoNumber = "PO-2026-REVERT-01",
            VendorId = vendor.Id,
            VendorName = vendor.Name,
            ResolvedDebtIds = new List<Guid> { debt.Id },
            PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    OrderedQuantity = 10,
                    UnitPrice = 0m,
                    TaxRate = 0
                }
            }
        });

        var unresolvedAfterCreate = await vendorDebtService.GetUnresolvedByVendorIdAsync(vendor.Id);
        Assert.Empty(unresolvedAfterCreate);

        var deleted = await poService.DeleteAsync(po.Id);
        Assert.True(deleted);

        var unresolvedAfterDelete = await vendorDebtService.GetUnresolvedByVendorIdAsync(vendor.Id);
        Assert.Single(unresolvedAfterDelete);
        var revertedDebt = unresolvedAfterDelete[0];
        Assert.False(revertedDebt.IsResolved);
        Assert.Null(revertedDebt.ResolvedInPoId);
        Assert.Null(revertedDebt.ResolvedDate);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_PreservesCreditOffsetAmount_RecalculatesTotalAmount()
    {
        var (_, poService, _, db, tenantId, _) = CreateTestContext();
        var (_, product, vendor, _) = await SeedMasterDataAsync(db, tenantId);

        var po = await poService.CreateAsync(new CreatePurchaseOrderDto
        {
            PoNumber = "PO-2026-UPD-OFFSET",
            VendorId = vendor.Id,
            VendorName = vendor.Name,
            CreditOffsetAmount = 200000m,
            PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    OrderedQuantity = 10,
                    UnitPrice = 50000m, // Raw: 500,000 -> Net: 300,000
                    TaxRate = 0
                }
            }
        });
        Assert.Equal(300000m, po.TotalAmount);

        // Update PO: increase quantity to 12 (Raw: 600,000 -> Net with 200k offset: 400,000)
        var updated = await poService.UpdateAsync(po.Id, new UpdatePurchaseOrderDto
        {
            VendorId = vendor.Id,
            VendorName = vendor.Name,
            CreditOffsetAmount = 200000m,
            PurchaseOrderDetails = new List<UpdatePurchaseOrderDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    OrderedQuantity = 12,
                    UnitPrice = 50000m,
                    TaxRate = 0
                }
            }
        });

        Assert.NotNull(updated);
        Assert.Equal(400000m, updated.TotalAmount);
        Assert.Equal(200000m, updated.CreditOffsetAmount);
    }

    [Fact]
    public async Task VendorDebtService_GetAllAsync_FiltersCorrectly()
    {
        var (_, _, vendorDebtService, db, tenantId, _) = CreateTestContext();
        var (_, product, vendor, receiving) = await SeedMasterDataAsync(db, tenantId);

        var goodsDebt = new VendorDebtLedger
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VendorId = vendor.Id,
            SourceReceivingId = receiving.Id,
            DebtType = VendorDebtType.GoodsBacklog,
            ProductId = product.Id,
            Quantity = 5,
            IsResolved = false,
            CreatedDate = DateTime.UtcNow
        };
        var creditDebt = new VendorDebtLedger
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VendorId = vendor.Id,
            SourceReceivingId = receiving.Id,
            DebtType = VendorDebtType.CreditBalance,
            Amount = 150000m,
            IsResolved = true,
            CreatedDate = DateTime.UtcNow
        };
        db.VendorDebtLedgers.AddRange(goodsDebt, creditDebt);
        await db.SaveChangesAsync();

        var allDebts = await vendorDebtService.GetAllAsync(vendor.Id, null, null);
        Assert.Equal(2, allDebts.Count);

        var unresolvedGoods = await vendorDebtService.GetAllAsync(vendor.Id, VendorDebtType.GoodsBacklog, false);
        Assert.Single(unresolvedGoods);
        Assert.Equal(goodsDebt.Id, unresolvedGoods[0].Id);

        var resolvedCredit = await vendorDebtService.GetAllAsync(vendor.Id, VendorDebtType.CreditBalance, true);
        Assert.Single(resolvedCredit);
        Assert.Equal(creditDebt.Id, resolvedCredit[0].Id);
    }
}
