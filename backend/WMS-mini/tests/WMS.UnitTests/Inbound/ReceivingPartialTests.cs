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

namespace WMS.UnitTests.Inbound;

public class ReceivingPartialTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    private static (ReceivingService receivingService, PurchaseOrderService poService, WmsDbContext db, Guid tenantId, Guid userId) CreateTestContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId, Guid.NewGuid().ToString());

        var mapper = CreateMapper();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.UserName).Returns("tester");
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(true);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() => db.SaveChangesAsync());
        uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        var receivingRepo = new SqlReceivingRepository(db);
        var putAwayRepo = new SqlPutAwayTaskRepository(db);
        var poRepo = new SqlPurchaseOrderRepository(db);

        var receivingService = new ReceivingService(receivingRepo, putAwayRepo, poRepo, uowMock.Object, currentUserMock.Object, mapper);
        var poService = new PurchaseOrderService(poRepo, mapper, uowMock.Object, currentUserMock.Object);

        return (receivingService, poService, db, tenantId, userId);
    }

    private static async Task<(PurchaseOrder po, Product product)> SeedApprovedPoAsync(WmsDbContext db, Guid tenantId, int orderedQuantity = 100, int receivedQuantity = 0)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Default Category",
            TenantId = tenantId
        };
        db.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = $"SKU-{Guid.NewGuid():N}"[..12],
            Name = "Test Product",
            CategoryId = category.Id,
            Price = 50000m,
            TenantId = tenantId
        };
        db.Products.Add(product);

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = $"PO-{Guid.NewGuid():N}"[..10],
            Status = receivedQuantity > 0 ? PurchaseOrderStatus.PartiallyReceived : PurchaseOrderStatus.Approved,
            TenantId = tenantId,
            PurchaseOrderDetails = new List<PurchaseOrderDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    OrderedQuantity = orderedQuantity,
                    ReceivedQuantity = receivedQuantity,
                    UnitPrice = 50000m,
                    TaxRate = 10m,
                    TenantId = tenantId
                }
            }
        };
        foreach (var d in po.PurchaseOrderDetails) d.PurchaseOrder = po;
        po.TotalAmount = po.PurchaseOrderDetails.Sum(d => d.OrderedQuantity * d.UnitPrice * (1 + d.TaxRate / 100m));

        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        return (po, product);
    }

    [Fact]
    public async Task ConfirmAsync_WhenPartialReceiving_ShouldSetPoStatusToPartiallyReceived_AndSetReceivedQuantity()
    {
        // Arrange
        var (receivingService, _, db, tenantId, _) = CreateTestContext();
        var (po, product) = await SeedApprovedPoAsync(db, tenantId, orderedQuantity: 100, receivedQuantity: 0);

        var createDto = new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.UtcNow,
            InvoiceTotalAmount = 2200000m,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 40,
                    ActualQuantity = 40,
                    Condition = ProductCondition.Ok,
                    UnitPrice = 50000m
                }
            }
        };

        var receiving = await receivingService.CreateAsync(createDto);

        // Act
        var confirmed = await receivingService.ConfirmAsync(receiving.Id);

        // Assert
        Assert.NotNull(confirmed);
        Assert.Equal(ReceivingStatus.Confirmed, confirmed.Status);
        Assert.Equal("INV-001", confirmed.InvoiceNumber);
        Assert.Equal(2200000m, confirmed.InvoiceTotalAmount);
        Assert.Single(confirmed.Details);
        Assert.Equal(50000m, confirmed.Details[0].UnitPrice);

        var savedReceiving = await db.Receivings.Include(r => r.ReceivingDetails).FirstOrDefaultAsync(r => r.Id == receiving.Id);
        Assert.NotNull(savedReceiving);
        Assert.Equal("INV-001", savedReceiving.InvoiceNumber);
        Assert.Equal(2200000m, savedReceiving.InvoiceTotalAmount);
        Assert.Equal(50000m, savedReceiving.ReceivingDetails.First().UnitPrice);

        var updatedPo = await db.PurchaseOrders.Include(p => p.PurchaseOrderDetails).FirstOrDefaultAsync(p => p.Id == po.Id);
        Assert.NotNull(updatedPo);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, updatedPo.Status);

        var detail = updatedPo.PurchaseOrderDetails.First(d => d.ProductId == product.Id);
        Assert.Equal(40, detail.ReceivedQuantity);

        var putAwayTasks = await db.PutAwayTasks.Where(t => t.ProductId == product.Id).ToListAsync();
        Assert.Single(putAwayTasks);
        Assert.Equal(40, putAwayTasks[0].Quantity);
    }

    [Fact]
    public async Task ConfirmAsync_WhenSecondBatchCompletesPo_ShouldSetPoStatusToReceived()
    {
        // Arrange
        var (receivingService, _, db, tenantId, _) = CreateTestContext();
        var (po, product) = await SeedApprovedPoAsync(db, tenantId, orderedQuantity: 100, receivedQuantity: 60);

        var createDto = new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            InvoiceNumber = "INV-002",
            InvoiceDate = DateTime.UtcNow,
            InvoiceTotalAmount = 2200000m,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 40,
                    ActualQuantity = 40,
                    Condition = ProductCondition.Ok,
                    UnitPrice = 50000m
                }
            }
        };

        var receiving = await receivingService.CreateAsync(createDto);

        // Act
        var confirmed = await receivingService.ConfirmAsync(receiving.Id);

        // Assert
        Assert.NotNull(confirmed);
        Assert.Equal(ReceivingStatus.Confirmed, confirmed.Status);

        var updatedPo = await db.PurchaseOrders.Include(p => p.PurchaseOrderDetails).FirstOrDefaultAsync(p => p.Id == po.Id);
        Assert.NotNull(updatedPo);
        Assert.Equal(PurchaseOrderStatus.Received, updatedPo.Status);

        var detail = updatedPo.PurchaseOrderDetails.First(d => d.ProductId == product.Id);
        Assert.Equal(100, detail.ReceivedQuantity);
    }

    [Fact]
    public async Task CreateAsync_WhenQuantityExceedsRemaining_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var (receivingService, _, db, tenantId, _) = CreateTestContext();
        var (po, product) = await SeedApprovedPoAsync(db, tenantId, orderedQuantity: 100, receivedQuantity: 60);

        var createDto = new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 50,
                    ActualQuantity = 50,
                    Condition = ProductCondition.Ok
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => receivingService.CreateAsync(createDto));
        Assert.Contains("vượt quá", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmAsync_WhenQuantityExceedsRemainingAtConfirmation_ShouldThrowInvalidOperationException()
    {
        // Arrange: PO had 100, receiving created for 50 when 100 were available.
        var (receivingService, _, db, tenantId, _) = CreateTestContext();
        var (po, product) = await SeedApprovedPoAsync(db, tenantId, orderedQuantity: 100, receivedQuantity: 0);

        var createDto = new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 50,
                    ActualQuantity = 50,
                    Condition = ProductCondition.Ok
                }
            }
        };

        var receiving = await receivingService.CreateAsync(createDto);

        // Simulate another receipt confirmed in parallel: received 70, remaining is now only 30.
        var poEntity = await db.PurchaseOrders.Include(p => p.PurchaseOrderDetails).FirstAsync(p => p.Id == po.Id);
        poEntity.PurchaseOrderDetails.First().ReceivedQuantity = 70;
        await db.SaveChangesAsync();

        // Act & Assert: attempting to confirm the 50 qty receipt should fail because 50 > 30.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => receivingService.ConfirmAsync(receiving.Id));
        Assert.Contains("vượt quá", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_WhenInvoiceTotalAmountNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var (receivingService, _, db, tenantId, _) = CreateTestContext();
        var (po, product) = await SeedApprovedPoAsync(db, tenantId, orderedQuantity: 100, receivedQuantity: 0);

        var createDto = new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            InvoiceNumber = "INV-NEG",
            InvoiceTotalAmount = -50000m,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 10,
                    Condition = ProductCondition.Ok
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => receivingService.CreateAsync(createDto));
        Assert.Contains("không được nhỏ hơn 0", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PurchaseOrderService_CreateAsync_ShouldCalculateTotalAmount_WithUnitPriceAndTaxRate()
    {
        // Arrange
        var (_, poService, db, tenantId, _) = CreateTestContext();

        var category = new Category { Id = Guid.NewGuid(), Name = "Cat1", TenantId = tenantId };
        db.Categories.Add(category);

        var prod1 = new Product { Id = Guid.NewGuid(), Sku = "SKU-001", Name = "P1", CategoryId = category.Id, TenantId = tenantId };
        var prod2 = new Product { Id = Guid.NewGuid(), Sku = "SKU-002", Name = "P2", CategoryId = category.Id, TenantId = tenantId };
        db.Products.AddRange(prod1, prod2);
        await db.SaveChangesAsync();

        var createDto = new CreatePurchaseOrderDto
        {
            PoNumber = "PO-CALC-001",
            VendorName = "Supplier A",
            PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
            {
                // 10 * 50,000 * (1 + 10/100) = 550,000
                new() { ProductId = prod1.Id, OrderedQuantity = 10, UnitPrice = 50000m, TaxRate = 10m },
                // 5 * 20,000 * (1 + 0/100) = 100,000
                new() { ProductId = prod2.Id, OrderedQuantity = 5, UnitPrice = 20000m, TaxRate = 0m }
            }
        };

        // Act
        var createdPo = await poService.CreateAsync(createDto);

        // Assert
        Assert.NotNull(createdPo);
        Assert.Equal(650000m, createdPo.TotalAmount);

        var entity = await db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == createdPo.Id);
        Assert.NotNull(entity);
        Assert.Equal(650000m, entity.TotalAmount);
    }

    [Fact]
    public async Task Location_CanBeCreatedWithLocationTypeQuarantine()
    {
        // Arrange
        var (_, _, db, tenantId, _) = CreateTestContext();
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Code = "WH-TEST", Name = "Warehouse Test", TenantId = tenantId };
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();

        var location = new Location
        {
            Id = Guid.NewGuid(),
            Code = "LOC-Q-01",
            Aisle = "Q",
            Rack = "01",
            Level = "1",
            LocationType = LocationType.Quarantine,
            WarehouseId = warehouse.Id,
            TenantId = tenantId
        };

        // Act
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        // Assert
        var savedLoc = await db.Locations.FirstOrDefaultAsync(l => l.Id == location.Id);
        Assert.NotNull(savedLoc);
        Assert.Equal(LocationType.Quarantine, savedLoc.LocationType);
    }

    [Fact]
    public async Task ConfirmAsync_WhenMultiItemPoHasOneItemFulfilledAndOneItemPending_ShouldRemainPartiallyReceived()
    {
        // Arrange
        var (receivingService, _, db, tenantId, _) = CreateTestContext();

        var category = new Category { Id = Guid.NewGuid(), Name = "Category Multi", TenantId = tenantId };
        db.Categories.Add(category);

        var p1 = new Product { Id = Guid.NewGuid(), Sku = "SKU-M1", Name = "P1", CategoryId = category.Id, TenantId = tenantId };
        var p2 = new Product { Id = Guid.NewGuid(), Sku = "SKU-M2", Name = "P2", CategoryId = category.Id, TenantId = tenantId };
        db.Products.AddRange(p1, p2);

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = "PO-MULTI-001",
            Status = PurchaseOrderStatus.Approved,
            TenantId = tenantId,
            PurchaseOrderDetails = new List<PurchaseOrderDetail>
            {
                new() { Id = Guid.NewGuid(), ProductId = p1.Id, OrderedQuantity = 50, ReceivedQuantity = 0, UnitPrice = 10000m, TaxRate = 0m, TenantId = tenantId },
                new() { Id = Guid.NewGuid(), ProductId = p2.Id, OrderedQuantity = 30, ReceivedQuantity = 0, UnitPrice = 20000m, TaxRate = 0m, TenantId = tenantId }
            }
        };
        foreach (var d in po.PurchaseOrderDetails) d.PurchaseOrder = po;
        po.TotalAmount = po.PurchaseOrderDetails.Sum(d => d.OrderedQuantity * d.UnitPrice);
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        // Receive full 50 of P1 only
        var createDto = new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new() { ProductId = p1.Id, ExpectedQuantity = 50, ActualQuantity = 50, Condition = ProductCondition.Ok, UnitPrice = 10000m }
            }
        };
        var receiving = await receivingService.CreateAsync(createDto);

        // Act
        await receivingService.ConfirmAsync(receiving.Id);

        // Assert: P1 is fully received (50/50), but P2 is unreceived (0/30) => PO must stay PartiallyReceived
        var updatedPo = await db.PurchaseOrders.Include(p => p.PurchaseOrderDetails).FirstAsync(p => p.Id == po.Id);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, updatedPo.Status);
        Assert.Equal(50, updatedPo.PurchaseOrderDetails.First(d => d.ProductId == p1.Id).ReceivedQuantity);
        Assert.Equal(0, updatedPo.PurchaseOrderDetails.First(d => d.ProductId == p2.Id).ReceivedQuantity);
    }

    [Fact]
    public async Task PurchaseOrderService_UpdateAsync_ShouldRecalculateTotalAmount()
    {
        // Arrange
        var (_, poService, db, tenantId, _) = CreateTestContext();

        var category = new Category { Id = Guid.NewGuid(), Name = "Category Update", TenantId = tenantId };
        db.Categories.Add(category);

        var prod = new Product { Id = Guid.NewGuid(), Sku = "SKU-UPD", Name = "P Update", CategoryId = category.Id, TenantId = tenantId };
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var createDto = new CreatePurchaseOrderDto
        {
            PoNumber = "PO-UPD-001",
            VendorName = "Supplier Init",
            PurchaseOrderDetails = new List<CreatePurchaseOrderDetailDto>
            {
                new() { ProductId = prod.Id, OrderedQuantity = 10, UnitPrice = 10000m, TaxRate = 10m } // 10 * 10,000 * 1.1 = 110,000
            }
        };
        var created = await poService.CreateAsync(createDto);
        Assert.Equal(110000m, created.TotalAmount);

        // Act: Update quantity to 20 and price to 15,000 => 20 * 15,000 * 1.1 = 330,000
        var updateDto = new UpdatePurchaseOrderDto
        {
            VendorName = "Supplier Updated",
            PurchaseOrderDetails = new List<UpdatePurchaseOrderDetailDto>
            {
                new() { ProductId = prod.Id, OrderedQuantity = 20, UnitPrice = 15000m, TaxRate = 10m }
            }
        };
        var updated = await poService.UpdateAsync(created.Id, updateDto);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(330000m, updated.TotalAmount);

        var entity = await db.PurchaseOrders.FirstAsync(p => p.Id == created.Id);
        Assert.Equal(330000m, entity.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_WhenDetailUnitPriceNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var (receivingService, _, db, tenantId, _) = CreateTestContext();
        var (po, product) = await SeedApprovedPoAsync(db, tenantId, orderedQuantity: 100, receivedQuantity: 0);

        var createDto = new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 10,
                    Condition = ProductCondition.Ok,
                    UnitPrice = -100m
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => receivingService.CreateAsync(createDto));
        Assert.Contains("Đơn giá không được nhỏ hơn 0", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PurchaseOrderService_CloseAsync_WhenPartiallyReceived_ShouldSucceed()
    {
        // Arrange
        var (_, poService, db, tenantId, _) = CreateTestContext();
        var (po, _) = await SeedApprovedPoAsync(db, tenantId, orderedQuantity: 100, receivedQuantity: 50);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, po.Status);

        // Act
        var closed = await poService.CloseAsync(po.Id);

        // Assert
        Assert.NotNull(closed);
        Assert.Equal(PurchaseOrderStatus.Closed, closed.Status);

        var entity = await db.PurchaseOrders.FirstAsync(p => p.Id == po.Id);
        Assert.Equal(PurchaseOrderStatus.Closed, entity.Status);
    }
}
