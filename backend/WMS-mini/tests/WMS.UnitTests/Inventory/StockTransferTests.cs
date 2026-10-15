using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Services;
using Xunit;

namespace WMS.UnitTests.Inventory;

public class StockTransferTests
{
    private static WmsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new WmsDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_WhenSameWarehouse_ThrowsException()
    {
        using var db = CreateInMemoryDbContext();
        var uow = new EfUnitOfWork(db);
        var currentUserMock = new Mock<ICurrentUserService>();
        var loggerMock = new Mock<ILogger<StockTransferService>>();

        var service = new StockTransferService(db, uow, currentUserMock.Object, loggerMock.Object);

        var sameWhId = Guid.NewGuid();
        var dto = new CreateStockTransferDto
        {
            FromWarehouseId = sameWhId,
            ToWarehouseId = sameWhId,
            Details = new List<CreateStockTransferDetailDto>
            {
                new() { ProductId = Guid.NewGuid(), FromLocationId = Guid.NewGuid(), TransferQty = 5 }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task ShipAsync_WhenAvailableStockInsufficient_ThrowsException()
    {
        using var db = CreateInMemoryDbContext();
        var uow = new EfUnitOfWork(db);
        var currentUserMock = new Mock<ICurrentUserService>();
        var loggerMock = new Mock<ILogger<StockTransferService>>();

        var service = new StockTransferService(db, uow, currentUserMock.Object, loggerMock.Object);

        var fromWh = new Warehouse { Id = Guid.NewGuid(), Name = "Kho HCM", Code = "WH-HCM" };
        var toWh = new Warehouse { Id = Guid.NewGuid(), Name = "Kho HN", Code = "WH-HN" };
        var prod = new Product { Id = Guid.NewGuid(), Sku = "SKU-1", Name = "Sáº£n pháº©m 1" };
        var loc = new Location { Id = Guid.NewGuid(), Code = "A-01", WarehouseId = fromWh.Id, CurrentQuantity = 10 };

        db.Warehouses.AddRange(fromWh, toWh);
        db.Products.Add(prod);
        db.Locations.Add(loc);
        db.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            ProductId = prod.Id,
            LocationId = loc.Id,
            OnhandQty = 10,
            ReservedQty = 8 // Available = 2
        });

        await db.SaveChangesAsync();

        var createDto = new CreateStockTransferDto
        {
            FromWarehouseId = fromWh.Id,
            ToWarehouseId = toWh.Id,
            Details = new List<CreateStockTransferDetailDto>
            {
                new() { ProductId = prod.Id, FromLocationId = loc.Id, TransferQty = 5 } // Cáº§n 5 > 2
            }
        };

        var created = await service.CreateAsync(createDto);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ShipAsync(created.Id));
    }

    [Fact]
    public async Task ShipAndReceive_FullLifecycle_SucceedsWithStockMovements()
    {
        using var db = CreateInMemoryDbContext();
        var uow = new EfUnitOfWork(db);
        var currentUserMock = new Mock<ICurrentUserService>();
        var loggerMock = new Mock<ILogger<StockTransferService>>();

        var service = new StockTransferService(db, uow, currentUserMock.Object, loggerMock.Object);

        var fromWh = new Warehouse { Id = Guid.NewGuid(), Name = "Kho HCM", Code = "WH-HCM" };
        var toWh = new Warehouse { Id = Guid.NewGuid(), Name = "Kho HN", Code = "WH-HN" };
        var prod = new Product { Id = Guid.NewGuid(), Sku = "SKU-1", Name = "Sáº£n pháº©m 1" };
        var fromLoc = new Location { Id = Guid.NewGuid(), Code = "HCM-A1", WarehouseId = fromWh.Id, CurrentQuantity = 10 };
        var toLoc = new Location { Id = Guid.NewGuid(), Code = "HN-B1", WarehouseId = toWh.Id, CurrentQuantity = 0 };

        db.Warehouses.AddRange(fromWh, toWh);
        db.Products.Add(prod);
        db.Locations.AddRange(fromLoc, toLoc);
        db.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            ProductId = prod.Id,
            LocationId = fromLoc.Id,
            OnhandQty = 10,
            ReservedQty = 0
        });

        await db.SaveChangesAsync();

        // 1. Táº¡o Draft
        var created = await service.CreateAsync(new CreateStockTransferDto
        {
            FromWarehouseId = fromWh.Id,
            ToWarehouseId = toWh.Id,
            Details = new List<CreateStockTransferDetailDto>
            {
                new() { ProductId = prod.Id, FromLocationId = fromLoc.Id, TransferQty = 4 }
            }
        });

        Assert.Equal(StockTransferStatus.Draft, created.Status);

        // 2. Xuáº¥t kho -> InTransit
        var shipped = await service.ShipAsync(created.Id);
        Assert.Equal(StockTransferStatus.InTransit, shipped.Status);

        var fromStock = await db.Stocks.FirstAsync(s => s.LocationId == fromLoc.Id);
        Assert.Equal(6, fromStock.OnhandQty); // 10 - 4 = 6

        var outMovement = await db.StockMovements.FirstOrDefaultAsync(m => m.MovementType == MovementType.TransferOut);
        Assert.NotNull(outMovement);
        Assert.Equal(-4, outMovement.Qty);

        // 3. Kho Ä‘Ã­ch nháº­n hÃ ng -> Completed
        var receiveDto = new ReceiveStockTransferDto
        {
            Items = new List<ReceiveStockTransferItemDto>
            {
                new() { DetailId = shipped.Details[0].Id, ToLocationId = toLoc.Id, ReceivedQty = 4 }
            }
        };

        var completed = await service.ReceiveAsync(created.Id, receiveDto);
        Assert.Equal(StockTransferStatus.Completed, completed.Status);

        var toStock = await db.Stocks.FirstAsync(s => s.LocationId == toLoc.Id);
        Assert.Equal(4, toStock.OnhandQty); // 0 + 4 = 4

        var inMovement = await db.StockMovements.FirstOrDefaultAsync(m => m.MovementType == MovementType.TransferIn);
        Assert.NotNull(inMovement);
        Assert.Equal(4, inMovement.Qty);
    }


    [Fact]
    public async Task CancelAsync_WhenDraft_SetsCancelled()
    {
        using var db = CreateInMemoryDbContext();
        var uow = new EfUnitOfWork(db);
        var currentUserMock = new Mock<ICurrentUserService>();
        var loggerMock = new Mock<ILogger<StockTransferService>>();

        var service = new StockTransferService(db, uow, currentUserMock.Object, loggerMock.Object);

        var fromWh = new Warehouse { Id = Guid.NewGuid(), Name = "Kho HCM", Code = "WH-HCM" };
        var toWh = new Warehouse { Id = Guid.NewGuid(), Name = "Kho HN", Code = "WH-HN" };
        var prod = new Product { Id = Guid.NewGuid(), Sku = "SKU-1", Name = "Sản phẩm 1" };
        var loc = new Location { Id = Guid.NewGuid(), Code = "A-01", WarehouseId = fromWh.Id, CurrentQuantity = 10 };

        db.Warehouses.AddRange(fromWh, toWh);
        db.Products.Add(prod);
        db.Locations.Add(loc);
        await db.SaveChangesAsync();

        var created = await service.CreateAsync(new CreateStockTransferDto
        {
            FromWarehouseId = fromWh.Id,
            ToWarehouseId = toWh.Id,
            Details = new List<CreateStockTransferDetailDto>
            {
                new() { ProductId = prod.Id, FromLocationId = loc.Id, TransferQty = 3 }
            }
        });

        var success = await service.CancelAsync(created.Id);
        Assert.True(success);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(StockTransferStatus.Cancelled, fetched.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenQuantityNonPositive_ThrowsException()
    {
        using var db = CreateInMemoryDbContext();
        var uow = new EfUnitOfWork(db);
        var currentUserMock = new Mock<ICurrentUserService>();
        var loggerMock = new Mock<ILogger<StockTransferService>>();
        var service = new StockTransferService(db, uow, currentUserMock.Object, loggerMock.Object);

        var fromWh = new Warehouse { Id = Guid.NewGuid(), Name = "Kho HCM", Code = "WH-HCM" };
        var toWh = new Warehouse { Id = Guid.NewGuid(), Name = "Kho HN", Code = "WH-HN" };
        db.Warehouses.AddRange(fromWh, toWh);
        await db.SaveChangesAsync();

        var dto = new CreateStockTransferDto
        {
            FromWarehouseId = fromWh.Id,
            ToWarehouseId = toWh.Id,
            Details = new List<CreateStockTransferDetailDto>
            {
                new() { ProductId = Guid.NewGuid(), FromLocationId = Guid.NewGuid(), TransferQty = 0 }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
    }

}