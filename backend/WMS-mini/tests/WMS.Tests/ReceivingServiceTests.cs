using WMS.Application.DTOs;
using WMS.Application.Services;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.TestSupport;
using Xunit;

namespace WMS.Tests;

public class ReceivingServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _poId = Guid.NewGuid();

    private ReceivingService BuildService(
        out FakePurchaseOrderRepository poRepo,
        out FakeReceivingRepository receivingRepo,
        out FakePutAwayTaskRepository putAwayRepo,
        out FakeUnitOfWork uow)
    {
        poRepo = new FakePurchaseOrderRepository();
        receivingRepo = new FakeReceivingRepository();
        putAwayRepo = new FakePutAwayTaskRepository();
        uow = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(_userId);
        return new ReceivingService(
            receivingRepo, putAwayRepo, poRepo, uow, currentUser, MapperFactory.Create());
    }

    private PurchaseOrder CreateApprovedPo(int ordered = 10, int received = 0)
    {
        var po = new PurchaseOrder
        {
            Id = _poId,
            PoNumber = "PO-001",
            Status = PurchaseOrderStatus.Approved,
            PurchaseOrderDetails = new List<PurchaseOrderDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = _poId,
                    ProductId = _productId,
                    Product = new Product { Id = _productId, Sku = "SKU-A", Name = "Sản phẩm A" },
                    OrderedQuantity = ordered,
                    ReceivedQuantity = received,
                },
            },
        };
        return po;
    }

    private Receiving CreateReceivingEntity(PurchaseOrder po, IReadOnlyList<(int actual, ProductCondition condition)> details, ReceivingStatus status = ReceivingStatus.Draft)
    {
        return new Receiving
        {
            Id = Guid.NewGuid(),
            ReceivingNo = "RC-001",
            PurchaseOrderId = po.Id,
            PurchaseOrder = po,
            Status = status,
            ReceivedById = _userId,
            ReceivedDate = DateTime.UtcNow,
            ReceivingDetails = details.Select((d, i) => new ReceivingDetail
            {
                Id = Guid.NewGuid(),
                ReceivingId = Guid.NewGuid(),
                ProductId = _productId,
                Product = po.PurchaseOrderDetails.First().Product,
                ActualQuantity = d.actual,
                Condition = d.condition,
            }).ToList(),
        };
    }

    [Fact]
    public async Task Create_QuantityExceedsRemainingOfPo_Throws()
    {
        var service = BuildService(out var poRepo, out _, out _, out _);
        poRepo.Items.Add(CreateApprovedPo(ordered: 10, received: 0));

        var dto = new CreateReceivingDto
        {
            PurchaseOrderId = _poId,
            Details = new List<CreateReceivingDetailDto>
            {
                new() { ProductId = _productId, ExpectedQuantity = 10, ActualQuantity = 15, Condition = ProductCondition.Ok },
            },
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        Assert.Contains("cần nhận là 10", ex.Message);
    }

    [Fact]
    public async Task Create_SameProductTwiceInOneReceiving_IsAllowedWhenUnderRemaining()
    {
        // PO đặt 10; hai dòng cùng sản phẩm (6 + 3) không vượt tổng còn lại.
        var service = BuildService(out var poRepo, out _, out _, out _);
        poRepo.Items.Add(CreateApprovedPo(ordered: 10, received: 0));

        var dto = new CreateReceivingDto
        {
            PurchaseOrderId = _poId,
            Details = new List<CreateReceivingDetailDto>
            {
                new() { ProductId = _productId, ExpectedQuantity = 6, ActualQuantity = 6, Condition = ProductCondition.Ok },
                new() { ProductId = _productId, ExpectedQuantity = 3, ActualQuantity = 3, Condition = ProductCondition.Ok },
            },
        };

        var result = await service.CreateAsync(dto);
        Assert.NotNull(result);
        Assert.Equal(2, result.Details.Count);
    }

    [Fact]
    public async Task Confirm_PartialQuantityCompletingPo_Throws()
    {
        // Kịch bản 02c: PO đặt 10, phiếu chỉ nhận 9 → không thể confirm vì chưa đủ.
        var service = BuildService(out var poRepo, out var receivingRepo, out _, out _);
        var po = CreateApprovedPo(ordered: 10, received: 0);
        poRepo.Items.Add(po);
        var receiving = CreateReceivingEntity(po, new[] { (9, ProductCondition.Ok) });
        receivingRepo.Items.Add(receiving);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmAsync(receiving.Id));
        Assert.Contains("khớp với số lượng còn lại", ex.Message);
    }

    [Fact]
    public async Task Confirm_SecondConfirmedReceivingForSamePo_Throws()
    {
        // Một PO chỉ có tối đa một receiving Confirmed.
        var service = BuildService(out var poRepo, out var receivingRepo, out _, out _);
        var po = CreateApprovedPo(ordered: 10, received: 0);
        poRepo.Items.Add(po);

        var first = CreateReceivingEntity(po, new[] { (10, ProductCondition.Ok) }, ReceivingStatus.Confirmed);
        var second = CreateReceivingEntity(po, new[] { (10, ProductCondition.Ok) });
        receivingRepo.Items.Add(first);
        receivingRepo.Items.Add(second);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmAsync(second.Id));
        Assert.Contains("đã có phiếu nhận được xác nhận", ex.Message);
    }

    [Fact]
    public async Task Confirm_OnlyOkCondition_CreatesPutAway_AndCountsReceived()
    {
        // PO 10; phiếu nhận 4 Ok + 6 Damaged. Confirm thành công nhưng chỉ dòng Ok
        // được cộng ReceivedQuantity và sinh PutAway; dòng Damaged không sinh PutAway.
        var service = BuildService(out var poRepo, out var receivingRepo, out var putAwayRepo, out _);
        var po = CreateApprovedPo(ordered: 10, received: 0);
        poRepo.Items.Add(po);
        var receiving = CreateReceivingEntity(po, new[] { (4, ProductCondition.Ok), (6, ProductCondition.Damaged) });
        receivingRepo.Items.Add(receiving);

        await service.ConfirmAsync(receiving.Id);

        var poDetail = po.PurchaseOrderDetails.First();
        Assert.Equal(4, poDetail.ReceivedQuantity);
        Assert.Equal(PurchaseOrderStatus.Received, po.Status);

        // Chỉ 1 PutAway cho dòng Ok (qty 4); dòng Damaged không tạo.
        var putAway = Assert.Single(putAwayRepo.Items);
        Assert.Equal(4, putAway.Quantity);
        Assert.Equal(PutAwayTaskStatus.Open, putAway.Status);
    }

    [Fact]
    public async Task Confirm_ReceivingStatusBecomesConfirmed_AndPurchaseOrderReceived()
    {
        var service = BuildService(out var poRepo, out var receivingRepo, out _, out _);
        var po = CreateApprovedPo(ordered: 10, received: 0);
        poRepo.Items.Add(po);
        var receiving = CreateReceivingEntity(po, new[] { (10, ProductCondition.Ok) });
        receivingRepo.Items.Add(receiving);

        var result = await service.ConfirmAsync(receiving.Id);

        Assert.Equal(ReceivingStatus.Confirmed, receiving.Status);
        Assert.Equal(_userId, receiving.ConfirmedById);
        Assert.NotNull(receiving.ConfirmedDate);
        Assert.Equal(PurchaseOrderStatus.Received, po.Status);
        Assert.Equal(10, po.PurchaseOrderDetails.First().ReceivedQuantity);
    }
}