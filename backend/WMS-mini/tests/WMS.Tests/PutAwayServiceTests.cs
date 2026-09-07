using WMS.Application.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Services;
using WMS.Tests.TestSupport;
using Xunit;

namespace WMS.Tests;

public class PutAwayServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();
    private readonly Guid _receivingDetailId = Guid.NewGuid();
    private readonly Guid _poId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();

    // out params: (1 repo, 2 receivingRepo, 3 stockRepo, 4 movementRepo, 5 locationRepo, 6 poRepo, 7 uow)
    private PutAwayService BuildService(
        out FakePutAwayTaskRepository repo,
        out FakeReceivingRepository receivingRepo,
        out FakeStockRepository stockRepo,
        out FakeStockMovementRepository movementRepo,
        out FakeLocationRepository locationRepo,
        out FakePurchaseOrderRepository poRepo,
        out FakeUnitOfWork uow)
    {
        repo = new FakePutAwayTaskRepository();
        stockRepo = new FakeStockRepository();
        movementRepo = new FakeStockMovementRepository();
        locationRepo = new FakeLocationRepository();
        poRepo = new FakePurchaseOrderRepository();
        uow = new FakeUnitOfWork();
        receivingRepo = new FakeReceivingRepository();
        var currentUser = new FakeCurrentUser(_userId);
        return new PutAwayService(
            repo, receivingRepo, poRepo, stockRepo, movementRepo, locationRepo,
            uow, currentUser, MapperFactory.Create());
    }

    private Location CreateLocation(int max = 100, int current = 0)
    {
        return new Location
        {
            Id = _locationId,
            WarehouseId = _warehouseId,
            Code = "A-01-01",
            MaxQuantity = max,
            CurrentQuantity = current,
        };
    }

    private ReceivingDetail CreateReceivingDetail(int actualQuantity)
    {
        return new ReceivingDetail
        {
            Id = _receivingDetailId,
            ReceivingId = Guid.NewGuid(),
            ProductId = _productId,
            Product = new Product { Id = _productId, Sku = "SKU-A", Name = "Sản phẩm A" },
            ActualQuantity = actualQuantity,
            Receiving = new Receiving
            {
                PurchaseOrderId = _poId,
                ReceivingNo = "RC-001",
            },
        };
    }

    private static PutAwayTask CreateTask(Location location, ReceivingDetail detail, int quantity, PutAwayTaskStatus status)
    {
        return new PutAwayTask
        {
            Id = Guid.NewGuid(),
            ReceivingDetailId = detail.Id,
            ReceivingDetail = detail,
            ProductId = detail.ProductId,
            Product = detail.Product,
            Quantity = quantity,
            ToLocationId = location.Id,
            ToLocation = location,
            Status = status,
            AssignToId = null,
        };
    }

    [Fact]
    public async Task Create_QuantityExceedsReceivingDetail_Throws()
    {
        var service = BuildService(out var repo, out var receivingRepo, out _, out _, out _, out _, out _);
        _ = repo;
        var detail = CreateReceivingDetail(actualQuantity: 10);
        var receiving = new Receiving
        {
            Id = Guid.NewGuid(),
            ReceivingNo = "RC-001",
            PurchaseOrderId = detail.Receiving.PurchaseOrderId,
            PurchaseOrder = new PurchaseOrder { Id = detail.Receiving.PurchaseOrderId, PoNumber = "PO-001" },
            ReceivingDetails = new List<ReceivingDetail> { detail },
        };
        receivingRepo.Items.Add(receiving);

        var dto = new CreatePutAwayTaskDto
        {
            ReceivingDetailId = detail.Id,
            ProductId = _productId,
            Quantity = 15,
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        Assert.Contains("Số lượng tối đa cho phép là 10", ex.Message);
    }

    [Fact]
    public async Task Assign_SetsAssignedByAndStatus()
    {
        var service = BuildService(out var repo, out _, out _, out _, out var locRepo, out _, out _);
        var location = CreateLocation();
        locRepo.Items.Add(location);
        var detail = CreateReceivingDetail(10);
        var task = CreateTask(location, detail, 4, PutAwayTaskStatus.Open);
        repo.Items.Add(task);

        var result = await service.AssignAsync(task.Id, _userId);

        Assert.Equal(_userId, task.AssignToId);
        Assert.Equal(_userId, task.AssignedById);
        Assert.Equal(PutAwayTaskStatus.Assigned, task.Status);
    }

    [Fact]
    public async Task StartProgress_CapacityExceeded_Throws()
    {
        var service = BuildService(out var repo, out _, out _, out _, out var locRepo, out _, out _);
        var location = CreateLocation(max: 100, current: 95);
        locRepo.Items.Add(location);
        var detail = CreateReceivingDetail(10);
        var task = CreateTask(location, detail, 20, PutAwayTaskStatus.Assigned);
        task.AssignToId = _userId;
        repo.Items.Add(task);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartProgressAsync(task.Id));
        Assert.Contains("không đủ sức chứa", ex.Message);
    }

    [Fact]
    public async Task StartProgress_WithoutDestinationLocation_Throws()
    {
        var service = BuildService(out var repo, out _, out _, out _, out _, out _, out _);
        var location = CreateLocation();
        var detail = CreateReceivingDetail(10);
        var task = CreateTask(location, detail, 5, PutAwayTaskStatus.Assigned);
        task.AssignToId = _userId;
        task.ToLocationId = null;
        task.ToLocation = null;
        repo.Items.Add(task);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartProgressAsync(task.Id));
        Assert.Contains("vị trí đích", ex.Message);
    }

    [Fact]
    public async Task Complete_AddsToExistingStock_MovesLocation_RecordsMovement()
    {
        var service = BuildService(out var repo, out _, out var stockRepo, out var movementRepo, out var locRepo, out _, out _);
        var location = CreateLocation(max: 100, current: 10);
        locRepo.Items.Add(location);
        var stock = new Stock { Id = Guid.NewGuid(), ProductId = _productId, LocationId = _locationId, OnhandQty = 5, ReservedQty = 0 };
        stockRepo.Items.Add(stock);

        var detail = CreateReceivingDetail(10);
        var task = CreateTask(location, detail, 4, PutAwayTaskStatus.InProgress);
        task.AssignToId = _userId;
        repo.Items.Add(task);

        await service.CompleteAsync(task.Id);

        Assert.Equal(9, stock.OnhandQty);
        Assert.Equal(14, location.CurrentQuantity);
        Assert.Equal(PutAwayTaskStatus.Completed, task.Status);
        Assert.Equal(_userId, task.CompletedById);

        var movement = Assert.Single(movementRepo.Items);
        Assert.Equal(MovementType.In, movement.MovementType);
        Assert.Equal(4, movement.Qty);
        Assert.Equal(_locationId, movement.LocationId);
    }

    [Fact]
    public async Task Complete_CreatesNewStockRow_WhenNoneExists()
    {
        var service = BuildService(out var repo, out _, out var stockRepo, out var movementRepo, out var locRepo, out _, out _);
        var location = CreateLocation(max: 100, current: 0);
        locRepo.Items.Add(location);

        var detail = CreateReceivingDetail(10);
        var task = CreateTask(location, detail, 7, PutAwayTaskStatus.InProgress);
        task.AssignToId = _userId;
        repo.Items.Add(task);

        await service.CompleteAsync(task.Id);

        var newStock = Assert.Single(stockRepo.Items);
        Assert.Equal(7, newStock.OnhandQty);
        Assert.Equal(_locationId, newStock.LocationId);
        Assert.Equal(7, location.CurrentQuantity);
        Assert.Single(movementRepo.Items);
    }

    [Fact]
    public async Task Complete_AutoClosesPurchaseOrder_WhenAllPutAwayDone()
    {
        var service = BuildService(out var repo, out _, out _, out _, out var locRepo, out var poRepo, out _);
        var location = CreateLocation(max: 100, current: 0);
        locRepo.Items.Add(location);

        var po = new PurchaseOrder
        {
            Id = _poId,
            PoNumber = "PO-001",
            Status = PurchaseOrderStatus.Received,
        };
        var poDetail = new PurchaseOrderDetail { ProductId = _productId, OrderedQuantity = 10, ReceivedQuantity = 10 };
        po.PurchaseOrderDetails.Add(poDetail);
        poRepo.Items.Add(po);

        var detail = CreateReceivingDetail(10);
        var task = CreateTask(location, detail, 10, PutAwayTaskStatus.InProgress);
        task.AssignToId = _userId;
        repo.Items.Add(task);

        await service.CompleteAsync(task.Id);

        Assert.Equal(PurchaseOrderStatus.Closed, po.Status);
        Assert.Equal(_userId, po.ClosedById);
        Assert.NotNull(po.ClosedDate);
    }
}