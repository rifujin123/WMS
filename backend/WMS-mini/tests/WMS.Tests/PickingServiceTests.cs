using WMS.Application.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Services;
using WMS.Tests.TestSupport;
using Xunit;

namespace WMS.Tests;

public class PickingServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();
    private readonly Guid _saleOrderId = Guid.NewGuid();

    private PickingService BuildService(
        out FakePickingRepository repo,
        out FakeSaleOrderRepository saleOrderRepo,
        out FakeStockRepository stockRepo,
        out FakeStockMovementRepository movementRepo,
        out FakeWarehouseRepository warehouseRepo,
        out FakeUnitOfWork uow)
    {
        repo = new FakePickingRepository();
        saleOrderRepo = new FakeSaleOrderRepository();
        stockRepo = new FakeStockRepository();
        movementRepo = new FakeStockMovementRepository();
        warehouseRepo = new FakeWarehouseRepository();
        uow = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(_userId);
        return new PickingService(
            repo, saleOrderRepo, stockRepo, movementRepo, warehouseRepo,
            uow, currentUser, MapperFactory.Create());
    }

    private Product CreateProduct() => new() { Id = _productId, Sku = "SKU-A", Name = "Sản phẩm A" };

    private Location CreateLocation() => new()
    {
        Id = _locationId,
        WarehouseId = _warehouseId,
        Code = "A-01-01",
        MaxQuantity = 100,
    };

    private Warehouse CreateWarehouse() => new() { Id = _warehouseId, Code = "WH-01", Name = "Kho chính" };

    private Stock CreateStock(int onhand = 10, int reserved = 4) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = _productId,
        Product = CreateProduct(),
        LocationId = _locationId,
        Location = CreateLocation(),
        OnhandQty = onhand,
        ReservedQty = reserved,
    };

    private SaleOrder CreateSaleOrder(int quantity = 4, SaleOrderDetailStatus detailStatus = SaleOrderDetailStatus.Pending, int allocated = 0)
    {
        return new SaleOrder
        {
            Id = _saleOrderId,
            OrderNo = "SO-001",
            Status = SaleOrderStatus.New,
            SaleOrderDetails = new List<SaleOrderDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SaleOrderId = _saleOrderId,
                    ProductId = _productId,
                    Product = CreateProduct(),
                    Quantity = quantity,
                    AllocatedQty = allocated,
                    Status = detailStatus,
                },
            },
        };
    }

    private Picking CreatePickingWithDetail(SaleOrder saleOrder, int qtyToPick = 4)
    {
        var picking = new Picking
        {
            Id = Guid.NewGuid(),
            PickingNo = "PICK-001",
            WarehouseId = _warehouseId,
            Warehouse = CreateWarehouse(),
            Status = PickingStatus.InProgress,
            AssignedToId = _userId,
        };
        var detail = new PickingDetail
        {
            Id = Guid.NewGuid(),
            PickingId = picking.Id,
            SaleOrderDetailId = saleOrder.SaleOrderDetails.First().Id,
            SaleOrderDetail = saleOrder.SaleOrderDetails.First(),
            ProductId = _productId,
            Product = CreateProduct(),
            LocationId = _locationId,
            QtyToPick = qtyToPick,
            QtyPicked = 0,
            Status = PickingDetailStatus.Pending,
        };
        detail.SaleOrderDetail.PickingDetails.Add(detail);
        picking.PickingDetails.Add(detail);
        return picking;
    }

    [Fact]
    public async Task Create_WithSufficientStock_AllocatesAndMarksOrderPicking()
    {
        var service = BuildService(out var repo, out var saleOrderRepo, out var stockRepo, out _, out var warehouseRepo, out _);
        warehouseRepo.Items.Add(CreateWarehouse());
        var saleOrder = CreateSaleOrder(quantity: 4);
        saleOrderRepo.Items.Add(saleOrder);
        stockRepo.Items.Add(CreateStock(onhand: 10, reserved: 0));

        var dto = new CreatePickingDto { SaleOrderId = _saleOrderId, WarehouseId = _warehouseId };
        var result = await service.CreateAsync(dto);

        var picking = Assert.Single(repo.Items);
        var detail = Assert.Single(picking.PickingDetails);
        Assert.Equal(4, detail.QtyToPick);
        Assert.Equal(_locationId, detail.LocationId);
        Assert.Equal(SaleOrderStatus.Picking, saleOrder.Status);
        Assert.Equal(4, stockRepo.Items.Single().ReservedQty);
        Assert.Equal(4, saleOrder.SaleOrderDetails.Single().AllocatedQty);
    }

    [Fact]
    public async Task Create_InsufficientStock_Throws()
    {
        var service = BuildService(out _, out var saleOrderRepo, out _, out _, out var warehouseRepo, out _);
        warehouseRepo.Items.Add(CreateWarehouse());
        saleOrderRepo.Items.Add(CreateSaleOrder(quantity: 4, allocated: 0));

        var dto = new CreatePickingDto { SaleOrderId = _saleOrderId, WarehouseId = _warehouseId };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        Assert.Contains("Không đủ tồn kho", ex.Message);
    }

    [Fact]
    public async Task Complete_DeductsStock_RecordsMovement_AndMarksOrderPacked()
    {
        var service = BuildService(out var repo, out var saleOrderRepo, out var stockRepo, out var movementRepo, out _, out _);
        var saleOrder = CreateSaleOrder(quantity: 4, detailStatus: SaleOrderDetailStatus.Pending, allocated: 4);
        saleOrderRepo.Items.Add(saleOrder);
        var stock = CreateStock(onhand: 10, reserved: 4);
        stockRepo.Items.Add(stock);
        var picking = CreatePickingWithDetail(saleOrder, qtyToPick: 4);
        repo.Items.Add(picking);

        var dto = new CompletePickingDto
        {
            Details = new List<CompletePickingDetailDto>
            {
                new() { DetailId = picking.PickingDetails.First().Id, QtyPicked = 4 },
            },
        };
        await service.CompleteAsync(picking.Id, dto);

        Assert.Equal(6, stock.OnhandQty);
        Assert.Equal(0, stock.ReservedQty);
        Assert.Equal(PickingStatus.Completed, picking.Status);
        Assert.Equal(SaleOrderStatus.Packed, saleOrder.Status);
        Assert.Equal(_userId, saleOrder.PackedById);
        Assert.NotNull(saleOrder.PackedDate);

        var movement = Assert.Single(movementRepo.Items);
        Assert.Equal(MovementType.Out, movement.MovementType);
        Assert.Equal(4, movement.Qty);
        Assert.Equal(_locationId, movement.LocationId);
        Assert.Equal(PickingDetailStatus.Picked, picking.PickingDetails.First().Status);
    }

    [Fact]
    public async Task Complete_QtyPickedMismatch_Throws()
    {
        var service = BuildService(out var repo, out var saleOrderRepo, out var stockRepo, out _, out _, out _);
        var saleOrder = CreateSaleOrder(quantity: 4, detailStatus: SaleOrderDetailStatus.Pending, allocated: 4);
        saleOrderRepo.Items.Add(saleOrder);
        stockRepo.Items.Add(CreateStock(onhand: 10, reserved: 4));
        var picking = CreatePickingWithDetail(saleOrder, qtyToPick: 4);
        repo.Items.Add(picking);

        var dto = new CompletePickingDto
        {
            Details = new List<CompletePickingDetailDto>
            {
                new() { DetailId = picking.PickingDetails.First().Id, QtyPicked = 3 },
            },
        };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(picking.Id, dto));
        Assert.Contains("khớp với số lượng yêu cầu", ex.Message);
        Assert.Equal(10, stockRepo.Items.Single().OnhandQty);
        Assert.Equal(PickingStatus.InProgress, picking.Status);
    }
}