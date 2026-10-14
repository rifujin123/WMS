using Microsoft.AspNetCore.Authorization;
using WMS.API.Controllers;
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

public class RoleSegregationAccountingTests
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
        PurchaseOrderService poService,
        SqlPurchaseOrderRepository poRepo,
        WmsDbContext db,
        Guid tenantId,
        Guid userId)
        CreateTestContext(string userRole, Guid? userWarehouseId = null)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId, Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.WarehouseId).Returns(userWarehouseId);
        currentUserMock.Setup(u => u.UserName).Returns("test_user");
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string[]>()))
            .Returns<string[]>(roles => roles.Contains(userRole));

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() => db.SaveChangesAsync());
        uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        var poRepo = new SqlPurchaseOrderRepository(db);
        var vendorDebtRepo = new SqlVendorDebtRepository(db);

        var poService = new PurchaseOrderService(
            poRepo,
            mapper,
            uowMock.Object,
            currentUserMock.Object,
            vendorDebtRepo);

        return (poService, poRepo, db, tenantId, userId);
    }

    [Fact]
    public async Task WarehouseManager_Cannot_ApplyCreditOffset_ThrowsException()
    {
        var (poService, _, db, tenantId, _) = CreateTestContext("WarehouseManager");

        var vendor = new Vendor { Id = Guid.NewGuid(), TenantId = tenantId, Name = "NCC Test" };
        var product = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU01", Name = "S?n ph?m 1" };
        db.Vendors.Add(vendor);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var dto = new CreatePurchaseOrderDto
        {
            PoNumber = "PO-WM-001",
            VendorId = vendor.Id,
            CreditOffsetAmount = 50000m,
            PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
            {
                new() { ProductId = product.Id, OrderedQuantity = 10, UnitPrice = 10000m, TaxRate = 0 }
            }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => poService.CreateAsync(dto));
        Assert.Contains("Quản lý kho không có thẩm quyền cấn trừ công nợ nhà cung cấp.", ex.Message);
    }

    [Fact]
    public async Task Accountant_Can_Create_PO_With_CreditOffset_And_Status_Is_Pending()
    {
        var (poService, _, db, tenantId, _) = CreateTestContext("Accountant");

        var vendor = new Vendor { Id = Guid.NewGuid(), TenantId = tenantId, Name = "NCC Test" };
        var product = new Product { Id = Guid.NewGuid(), TenantId = tenantId, Sku = "SKU01", Name = "S?n ph?m 1" };
        db.Vendors.Add(vendor);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var dto = new CreatePurchaseOrderDto
        {
            PoNumber = "PO-ACC-001",
            VendorId = vendor.Id,
            CreditOffsetAmount = 20000m,
            PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
            {
                new() { ProductId = product.Id, OrderedQuantity = 10, UnitPrice = 10000m, TaxRate = 0 }
            }
        };

        var result = await poService.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(PurchaseOrderStatus.Pending, result.Status);
        Assert.Equal(20000m, result.CreditOffsetAmount);
        Assert.Equal(80000m, result.TotalAmount);
    }

    [Fact]
    public async Task MakerChecker_ChiefAccountant_Can_Approve_PO()
    {
        var (poService, _, db, tenantId, chiefId) = CreateTestContext("ChiefAccountant");

        var vendor = new Vendor { Id = Guid.NewGuid(), TenantId = tenantId, Name = "NCC Test" };
        db.Vendors.Add(vendor);

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = "PO-MC-001",
            VendorId = vendor.Id,
            Status = PurchaseOrderStatus.Pending,
            TotalAmount = 100000m
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        var approved = await poService.ApproveAsync(po.Id);

        Assert.NotNull(approved);
        Assert.Equal(PurchaseOrderStatus.Approved, approved.Status);
        Assert.Equal(chiefId, approved.ApprovedById);
    }

    [Fact]
    public async Task DataScoping_Accountant_With_WarehouseId_Is_Filtered_To_Own_Warehouse()
    {
        var whA = Guid.NewGuid();
        var whB = Guid.NewGuid();
        var (poService, _, db, tenantId, _) = CreateTestContext("Accountant", userWarehouseId: whA);

        var po1 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = "PO-WHA-001",
            WarehouseId = whA,
            Status = PurchaseOrderStatus.Pending
        };
        var po2 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = "PO-WHB-001",
            WarehouseId = whB,
            Status = PurchaseOrderStatus.Pending
        };
        db.PurchaseOrders.AddRange(po1, po2);
        await db.SaveChangesAsync();

        var result = await poService.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("PO-WHA-001", result[0].PoNumber);
    }

    [Fact]
    public async Task DataScoping_ChiefAccountant_Sees_All_Warehouses()
    {
        var whA = Guid.NewGuid();
        var whB = Guid.NewGuid();
        var (poService, _, db, tenantId, _) = CreateTestContext("ChiefAccountant", userWarehouseId: null);

        var po1 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = "PO-WHA-002",
            WarehouseId = whA,
            Status = PurchaseOrderStatus.Pending
        };
        var po2 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = "PO-WHB-002",
            WarehouseId = whB,
            Status = PurchaseOrderStatus.Pending
        };
        db.PurchaseOrders.AddRange(po1, po2);
        await db.SaveChangesAsync();

        var result = await poService.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SqlPurchaseOrderRepository_Filters_By_CreatedById()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId, Guid.NewGuid().ToString());
        var repo = new SqlPurchaseOrderRepository(db);

        var creatorA = Guid.NewGuid();
        var creatorB = Guid.NewGuid();

        var po1 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = "PO-CREATOR-A",
            CreatedById = creatorA,
            CreatedDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Pending
        };
        var po2 = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = "PO-CREATOR-B",
            CreatedById = creatorB,
            CreatedDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Pending
        };
        db.PurchaseOrders.AddRange(po1, po2);
        await db.SaveChangesAsync();

        po1.CreatedById = creatorA;
        po2.CreatedById = creatorB;
        db.Entry(po1).Property(p => p.CreatedById).IsModified = true;
        db.Entry(po2).Property(p => p.CreatedById).IsModified = true;
        await db.SaveChangesAsync();

        var query = new PurchaseOrderListQuery { Page = 1, CreatedById = creatorA };
        var paged = await repo.GetPagedAsync(query, 10);

        Assert.Equal(1, paged.TotalCount);
        Assert.Equal("PO-CREATOR-A", paged.Items[0].PoNumber);
    }

    [Fact]
    public void Controller_Endpoint_Authorization_Attributes_Enforce_Role_Segregation()
    {
        // 1. VendorDebtsController: only Admin, ChiefAccountant, Accountant
        var debtAttr = typeof(VendorDebtsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(debtAttr);
        Assert.Equal("Admin,ChiefAccountant,Accountant", debtAttr.Roles);

        // 2. SaleOrdersController.ConfirmDeposit: only Admin, ChiefAccountant, Accountant
        var confirmDepositMethod = typeof(SaleOrdersController).GetMethod("ConfirmDeposit");
        var confirmDepositAttr = confirmDepositMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(confirmDepositAttr);
        Assert.Equal("Admin,ChiefAccountant,Accountant", confirmDepositAttr.Roles);

        // 3. PurchaseOrdersController.Approve: only Admin, ChiefAccountant
        var approveMethod = typeof(PurchaseOrdersController).GetMethod("Approve");
        var approveAttr = approveMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(approveAttr);
        Assert.Equal("Admin,ChiefAccountant", approveAttr.Roles);

        // 4. PurchaseOrdersController.Close: Admin, ChiefAccountant, WarehouseManager
        var closeMethod = typeof(PurchaseOrdersController).GetMethod("Close");
        var closeAttr = closeMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(closeAttr);
        Assert.Equal("Admin,ChiefAccountant,WarehouseManager", closeAttr.Roles);
    }
}