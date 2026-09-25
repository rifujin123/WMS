using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.UnitTests.Common;

namespace WMS.UnitTests.InboundOutbound;

public class OrderLifecycleTests
{
    [Fact]
    public async Task PurchaseOrder_ShouldFollowCorrectStatusTransitions()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var poId = Guid.NewGuid();

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            var po = new PurchaseOrder
            {
                Id = poId,
                PoNumber = "PO-2026-001",
                Status = PurchaseOrderStatus.Pending,
                TenantId = tenantId
            };
            db.PurchaseOrders.Add(po);
            await db.SaveChangesAsync();
        }

        // Act & Assert 1: Pending -> Approved
        using (var db1 = TestDbContextFactory.Create(tenantId, dbName))
        {
            var po = await db1.PurchaseOrders.FindAsync(poId);
            Assert.NotNull(po);
            Assert.Equal(PurchaseOrderStatus.Pending, po.Status);

            po.Status = PurchaseOrderStatus.Approved;
            await db1.SaveChangesAsync();
        }

        // Act & Assert 2: Approved -> Received
        using (var db2 = TestDbContextFactory.Create(tenantId, dbName))
        {
            var po = await db2.PurchaseOrders.FindAsync(poId);
            Assert.NotNull(po);
            Assert.Equal(PurchaseOrderStatus.Approved, po.Status);

            po.Status = PurchaseOrderStatus.Received;
            await db2.SaveChangesAsync();
        }

        // Act & Assert 3: Received -> Closed
        using (var db3 = TestDbContextFactory.Create(tenantId, dbName))
        {
            var po = await db3.PurchaseOrders.FindAsync(poId);
            Assert.NotNull(po);
            Assert.Equal(PurchaseOrderStatus.Received, po.Status);

            po.Status = PurchaseOrderStatus.Closed;
            await db3.SaveChangesAsync();
        }

        // Final verification
        using (var dbFinal = TestDbContextFactory.Create(tenantId, dbName))
        {
            var po = await dbFinal.PurchaseOrders.FindAsync(poId);
            Assert.NotNull(po);
            Assert.Equal(PurchaseOrderStatus.Closed, po.Status);
        }
    }

    [Fact]
    public async Task SaleOrder_ShouldFollowCorrectStatusTransitions()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var soId = Guid.NewGuid();

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            var so = new SaleOrder
            {
                Id = soId,
                OrderNo = "SO-2026-001",
                Status = SaleOrderStatus.New,
                TenantId = tenantId
            };
            db.SaleOrders.Add(so);
            await db.SaveChangesAsync();
        }

        // Act & Assert: New -> Allocated -> Picking -> Packed -> Shipped
        var transitions = new[]
        {
            SaleOrderStatus.Allocated,
            SaleOrderStatus.Picking,
            SaleOrderStatus.Packed,
            SaleOrderStatus.Shipped
        };

        foreach (var nextStatus in transitions)
        {
            using var dbStep = TestDbContextFactory.Create(tenantId, dbName);
            var so = await dbStep.SaleOrders.FindAsync(soId);
            Assert.NotNull(so);
            so.Status = nextStatus;
            await dbStep.SaveChangesAsync();
        }

        using (var dbFinal = TestDbContextFactory.Create(tenantId, dbName))
        {
            var so = await dbFinal.SaleOrders.FindAsync(soId);
            Assert.NotNull(so);
            Assert.Equal(SaleOrderStatus.Shipped, so.Status);
        }
    }

    [Fact]
    public async Task StockMovement_ShouldRecordAuditTrailForInboundAndOutbound()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            // Seed Inbound movement
            db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationId = locationId,
                MovementType = MovementType.In,
                Qty = 100,
                TenantId = tenantId,
                Notes = "Nhập kho theo PO-2026-001"
            });

            // Seed Outbound movement
            db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationId = locationId,
                MovementType = MovementType.Out,
                Qty = 25,
                TenantId = tenantId,
                Notes = "Xuất kho theo SO-2026-001"
            });

            await db.SaveChangesAsync();
        }

        // Act & Assert
        using (var dbQuery = TestDbContextFactory.Create(tenantId, dbName))
        {
            var movements = await dbQuery.StockMovements
                .Where(m => m.ProductId == productId)
                .OrderBy(m => m.MovementType)
                .ToListAsync();

            Assert.Equal(2, movements.Count);
            Assert.Contains(movements, m => m.MovementType == MovementType.In && m.Qty == 100);
            Assert.Contains(movements, m => m.MovementType == MovementType.Out && m.Qty == 25);
        }
    }
}
