using WMS.Application.DTOs;
using WMS.Domain.Entities;
using WMS.Infrastructure.Services;
using WMS.Tests.TestSupport;
using Xunit;

namespace WMS.Tests;

public class StockMovementServiceTests
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();

    private StockMovementService BuildService(out FakeStockMovementRepository repo)
    {
        repo = new FakeStockMovementRepository();
        return new StockMovementService(repo, MapperFactory.Create());
    }

    private static StockMovement CreateMovement(Guid productId, Guid locationId, int offsetMinutes)
    {
        return new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            LocationId = locationId,
            MovementType = Domain.Enums.MovementType.In,
            Qty = 5,
            CreatedDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(offsetMinutes),
        };
    }

    [Fact]
    public async Task GetAsync_ReturnsFirstPageWithTotal()
    {
        var service = BuildService(out var repo);
        for (var i = 0; i < 25; i++)
        {
            repo.Items.Add(CreateMovement(_productId, _locationId, i));
        }

        var result = await service.GetAsync(new StockMovementQueryDto { Page = 1 }, pageSize: 10);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        // Mới nhất trước
        Assert.True(result.Items[0].OccurredAtUtc > result.Items[1].OccurredAtUtc);
    }

    [Fact]
    public async Task GetAsync_SecondPageSkipsFirstPage()
    {
        var service = BuildService(out var repo);
        for (var i = 0; i < 25; i++)
        {
            repo.Items.Add(CreateMovement(_productId, _locationId, i));
        }

        var result = await service.GetAsync(new StockMovementQueryDto { Page = 3 }, pageSize: 10);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.Page);
    }
}