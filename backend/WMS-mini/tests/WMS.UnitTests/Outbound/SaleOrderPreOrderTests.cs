using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Application.Mappings;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Repositories;
using WMS.Infrastructure.Services;
using WMS.UnitTests.Common;

namespace WMS.UnitTests.Outbound;

public class SaleOrderPreOrderTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    private static (SaleOrderService saleOrderService, Mock<IPickingService> pickingMock, WmsDbContext db, Guid tenantId, Guid warehouseId, Product product, Customer customer)
        CreateTestContext()
    {
        var tenantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId, Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.WarehouseId).Returns(warehouseId);
        currentUserMock.Setup(u => u.IsInRole("Admin")).Returns(true);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() => db.SaveChangesAsync());

        var pickingMock = new Mock<IPickingService>();

        var repo = new SqlSaleOrderRepository(db);
        var productRepo = new SqlProductRepository(db);
        var customerRepo = new SqlCustomerRepository(db);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = "SKU-PRE-001",
            Name = "Sản phẩm thử nghiệm",
            TenantId = tenantId,
            Price = 100000m
        };
        db.Products.Add(product);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Công ty TNHH Khách Hàng Mẫu",
            TenantId = tenantId,
            Phone = "0901234567"
        };
        db.Customers.Add(customer);

        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Code = "WH-MAIN",
            Name = "Kho Tổng",
            TenantId = tenantId
        };
        db.Warehouses.Add(warehouse);

        db.SaveChanges();

        var service = new SaleOrderService(
            repo,
            productRepo,
            customerRepo,
            pickingMock.Object,
            mapper,
            uowMock.Object,
            currentUserMock.Object);

        return (service, pickingMock, db, tenantId, warehouseId, product, customer);
    }

    [Fact]
    public async Task CreateRegularSaleOrder_WithSufficientStock_ShouldCalculateTotalAmountAndCreatePicking()
    {
        // Arrange
        var (service, pickingMock, db, tenantId, warehouseId, product, customer) = CreateTestContext();
        pickingMock.Setup(p => p.CreateAsync(It.IsAny<CreatePickingDto>()))
            .ReturnsAsync(new PickingDto { Id = Guid.NewGuid() });

        var dto = new CreateSaleOrderDto
        {
            OrderNo = "SO-REG-001",
            CustomerId = customer.Id,
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = false,
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 3,
                    UnitPrice = 100000m
                }
            }
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SaleOrderStatus.New, result.Status);
        Assert.Equal(300000m, result.TotalAmount);
        Assert.Equal(customer.Name, result.CustomerName);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.False(result.IsPreOrder);
        pickingMock.Verify(p => p.CreateAsync(It.Is<CreatePickingDto>(x => x.SaleOrderId == result.Id)), Times.Once);

        var inDb = await db.SaleOrders.FindAsync(result.Id);
        Assert.NotNull(inDb);
        Assert.Equal(300000m, inDb.TotalAmount);
    }

    [Fact]
    public async Task CreateRegularSaleOrder_WhenInsufficientStock_ShouldRollbackAndThrow()
    {
        // Arrange
        var (service, pickingMock, db, tenantId, warehouseId, product, _) = CreateTestContext();
        pickingMock.Setup(p => p.CreateAsync(It.IsAny<CreatePickingDto>()))
            .ThrowsAsync(new InvalidOperationException("Không đủ tồn kho khả dụng cho sản phẩm 'SKU-PRE-001'."));

        var dto = new CreateSaleOrderDto
        {
            OrderNo = "SO-FAIL-001",
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = false,
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 10,
                    UnitPrice = 50000m
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        Assert.Contains("Không đủ tồn kho khả dụng", ex.Message);

        var savedOrder = await db.SaleOrders.FirstOrDefaultAsync(o => o.OrderNo == "SO-FAIL-001");
        Assert.Null(savedOrder);
    }

    [Fact]
    public async Task CreatePreOrder_WithoutDeposit_ShouldBeAwaitingDepositAndNotCreatePicking()
    {
        // Arrange
        var (service, pickingMock, db, tenantId, warehouseId, product, customer) = CreateTestContext();

        var dto = new CreateSaleOrderDto
        {
            OrderNo = "SO-PRE-001",
            CustomerId = customer.Id,
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = true,
            RequiredDeposit = 200000m,
            PaidDeposit = 0m,
            DepositDeadline = DateTime.UtcNow.AddDays(3),
            DepositNotes = "Khách hứa cọc trong 3 ngày",
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 2,
                    UnitPrice = 150000m
                }
            }
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsPreOrder);
        Assert.Equal(SaleOrderStatus.AwaitingDeposit, result.Status);
        Assert.Equal(300000m, result.TotalAmount);
        Assert.Equal(200000m, result.RequiredDeposit);
        Assert.Equal(0m, result.PaidDeposit);
        Assert.Equal(customer.Name, result.CustomerName);
        Assert.Equal("Khách hứa cọc trong 3 ngày", result.DepositNotes);

        pickingMock.Verify(p => p.CreateAsync(It.IsAny<CreatePickingDto>()), Times.Never);
    }

    [Fact]
    public async Task CreatePreOrder_WithFullDeposit_ShouldBeBackorderDirectly()
    {
        // Arrange
        var (service, pickingMock, db, tenantId, warehouseId, product, customer) = CreateTestContext();

        var dto = new CreateSaleOrderDto
        {
            OrderNo = "SO-PRE-PAID",
            CustomerId = customer.Id,
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = true,
            RequiredDeposit = 200000m,
            PaidDeposit = 200000m,
            DepositNotes = "Đã nhận chuyển khoản Vietcombank",
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 2,
                    UnitPrice = 150000m
                }
            }
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsPreOrder);
        Assert.Equal(SaleOrderStatus.Backorder, result.Status);
        Assert.Equal(200000m, result.PaidDeposit);
        pickingMock.Verify(p => p.CreateAsync(It.IsAny<CreatePickingDto>()), Times.Never);
    }

    [Fact]
    public async Task CreatePreOrder_WithInvalidDeposit_ShouldThrowError()
    {
        // Arrange
        var (service, _, _, _, warehouseId, product, _) = CreateTestContext();

        var dto = new CreateSaleOrderDto
        {
            OrderNo = "SO-INVALID-DEP",
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = true,
            RequiredDeposit = 0m, // Invalid!
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    UnitPrice = 100000m
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        Assert.Contains("Tiền cọc yêu cầu phải lớn hơn 0", ex.Message);
    }

    [Fact]
    public async Task ConfirmDeposit_OnAwaitingDepositOrder_ShouldTransitionToBackorder()
    {
        // Arrange
        var (service, _, db, _, warehouseId, product, customer) = CreateTestContext();

        var createDto = new CreateSaleOrderDto
        {
            OrderNo = "SO-CONFIRM-001",
            CustomerId = customer.Id,
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = true,
            RequiredDeposit = 200000m,
            PaidDeposit = 0m,
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 2,
                    UnitPrice = 120000m
                }
            }
        };

        var order = await service.CreateAsync(createDto);
        Assert.Equal(SaleOrderStatus.AwaitingDeposit, order.Status);

        // Act
        var confirmDto = new ConfirmDepositDto
        {
            PaidDeposit = 200000m,
            DepositNotes = "Đã xác nhận tiền về tài khoản Techcombank"
        };
        var updated = await service.ConfirmDepositAsync(order.Id, confirmDto);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(SaleOrderStatus.Backorder, updated.Status);
        Assert.Equal(200000m, updated.PaidDeposit);
        Assert.Equal("Đã xác nhận tiền về tài khoản Techcombank", updated.DepositNotes);

        var dbOrder = await db.SaleOrders.FindAsync(order.Id);
        Assert.NotNull(dbOrder);
        Assert.Equal(SaleOrderStatus.Backorder, dbOrder.Status);
        Assert.Equal(200000m, dbOrder.PaidDeposit);
    }

    [Fact]
    public async Task ConfirmDeposit_OnNonAwaitingDepositOrder_ShouldThrow()
    {
        // Arrange
        var (service, _, db, tenantId, _, product, _) = CreateTestContext();

        var order = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO-PICKING-001",
            Status = SaleOrderStatus.Picking,
            TenantId = tenantId
        };
        db.SaleOrders.Add(order);
        await db.SaveChangesAsync();

        var confirmDto = new ConfirmDepositDto
        {
            PaidDeposit = 100000m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmDepositAsync(order.Id, confirmDto));
        Assert.Contains("Chỉ có thể xác nhận cọc cho đơn ở trạng thái 'Chờ cọc'", ex.Message);
    }

    [Fact]
    public async Task ConfirmDeposit_WithPartialDeposit_ShouldAccumulateAndRemainAwaitingDeposit()
    {
        // Arrange
        var (service, _, db, _, warehouseId, product, customer) = CreateTestContext();

        var createDto = new CreateSaleOrderDto
        {
            OrderNo = "SO-PARTIAL-001",
            CustomerId = customer.Id,
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = true,
            RequiredDeposit = 200000m,
            PaidDeposit = 50000m,
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new() { ProductId = product.Id, Quantity = 2, UnitPrice = 120000m }
            }
        };

        var order = await service.CreateAsync(createDto);
        Assert.Equal(SaleOrderStatus.AwaitingDeposit, order.Status);
        Assert.Equal(50000m, order.PaidDeposit);

        // Act: Deposit another 50,000 (total 100,000 < 200,000 required)
        var confirmDto = new ConfirmDepositDto
        {
            PaidDeposit = 50000m,
            DepositNotes = "Khách cọc thêm đợt 2"
        };
        var updated = await service.ConfirmDepositAsync(order.Id, confirmDto);

        // Assert: PaidDeposit accumulated to 100k, status remains AwaitingDeposit
        Assert.NotNull(updated);
        Assert.Equal(SaleOrderStatus.AwaitingDeposit, updated.Status);
        Assert.Equal(100000m, updated.PaidDeposit);

        var dbOrder = await db.SaleOrders.FindAsync(order.Id);
        Assert.NotNull(dbOrder);
        Assert.Equal(SaleOrderStatus.AwaitingDeposit, dbOrder.Status);
        Assert.Equal(100000m, dbOrder.PaidDeposit);
    }

    [Fact]
    public async Task CreateSaleOrder_WithNegativeUnitPrice_ShouldThrow()
    {
        // Arrange
        var (service, _, _, _, warehouseId, product, _) = CreateTestContext();

        var dto = new CreateSaleOrderDto
        {
            OrderNo = "SO-NEG-PRICE",
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = false,
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new() { ProductId = product.Id, Quantity = 1, UnitPrice = -50000m }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        Assert.Contains("Đơn giá sản phẩm không được âm", ex.Message);
    }

    [Fact]
    public async Task CreateSaleOrder_WithoutCustomerId_ShouldPreserveCustomerName()
    {
        // Arrange
        var (service, pickingMock, db, _, warehouseId, product, _) = CreateTestContext();
        pickingMock.Setup(p => p.CreateAsync(It.IsAny<CreatePickingDto>()))
            .ReturnsAsync(new PickingDto { Id = Guid.NewGuid() });

        var dto = new CreateSaleOrderDto
        {
            OrderNo = "SO-RETAIL-001",
            CustomerId = null,
            CustomerName = "Khách Lẻ Vãng Lai",
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = false,
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new() { ProductId = product.Id, Quantity = 1, UnitPrice = 100000m }
            }
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.CustomerId);
        Assert.Equal("Khách Lẻ Vãng Lai", result.CustomerName);

        var dbOrder = await db.SaleOrders.FindAsync(result.Id);
        Assert.NotNull(dbOrder);
        Assert.Null(dbOrder.CustomerId);
        Assert.Equal("Khách Lẻ Vãng Lai", dbOrder.CustomerName);
    }

    [Fact]
    public async Task CreateSaleOrder_WithNonExistentCustomerId_ShouldThrow()
    {
        // Arrange
        var (service, _, _, _, warehouseId, product, _) = CreateTestContext();

        var dto = new CreateSaleOrderDto
        {
            OrderNo = "SO-GHOST-CUST",
            CustomerId = Guid.NewGuid(),
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = false,
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new() { ProductId = product.Id, Quantity = 1, UnitPrice = 100000m }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        Assert.Contains("Khách hàng không tồn tại", ex.Message);
    }

    [Fact]
    public async Task UpdateSaleOrder_ShouldRecalculateTotalAmountAndCustomer()
    {
        // Arrange
        var (service, pickingMock, db, _, warehouseId, product, customer) = CreateTestContext();
        pickingMock.Setup(p => p.CreateAsync(It.IsAny<CreatePickingDto>()))
            .ReturnsAsync(new PickingDto { Id = Guid.NewGuid() });

        var createDto = new CreateSaleOrderDto
        {
            OrderNo = "SO-UPD-001",
            CustomerId = customer.Id,
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = false,
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new() { ProductId = product.Id, Quantity = 2, UnitPrice = 100000m } // 200,000
            }
        };
        var created = await service.CreateAsync(createDto);
        Assert.Equal(200000m, created.TotalAmount);

        // Act: Update to 5 items with price 120,000 => 600,000
        var updateDto = new CreateSaleOrderDto
        {
            OrderNo = "SO-UPD-001",
            CustomerId = customer.Id,
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            IsPreOrder = false,
            SaleOrderDetails = new List<CreateSaleOrderDetailDto>
            {
                new() { ProductId = product.Id, Quantity = 5, UnitPrice = 120000m }
            }
        };
        var updated = await service.UpdateAsync(created.Id, updateDto);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(600000m, updated.TotalAmount);

        var dbOrder = await db.SaleOrders.FindAsync(created.Id);
        Assert.NotNull(dbOrder);
        Assert.Equal(600000m, dbOrder.TotalAmount);
    }
}
