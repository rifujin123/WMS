using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;
using WMS.UnitTests.Common;

namespace WMS.UnitTests.MasterData;

public class SkuUniquenessTests
{
    [Fact]
    public async Task ProductSku_CanBeReusedAcrossDifferentTenants()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var sharedSku = "IPHONE-15-PRO";

        // Tenant A creates product with sharedSku
        using (var dbA = TestDbContextFactory.Create(tenantA, dbName))
        {
            dbA.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = "iPhone 15 Pro (Shop A)",
                Sku = sharedSku,
                TenantId = tenantA
            });
            await dbA.SaveChangesAsync();
        }

        // Tenant B creates product with the EXACT SAME sharedSku
        using (var dbB = TestDbContextFactory.Create(tenantB, dbName))
        {
            dbB.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = "iPhone 15 Pro (Shop B)",
                Sku = sharedSku,
                TenantId = tenantB
            });
            await dbB.SaveChangesAsync();
        }

        // Assert: Cả 2 tenant đều lưu trữ thành công sản phẩm với cùng mã SKU
        using (var dbQueryA = TestDbContextFactory.Create(tenantA, dbName))
        {
            var productA = await dbQueryA.Products.FirstOrDefaultAsync(p => p.Sku == sharedSku);
            Assert.NotNull(productA);
            Assert.Equal("iPhone 15 Pro (Shop A)", productA.Name);
        }

        using (var dbQueryB = TestDbContextFactory.Create(tenantB, dbName))
        {
            var productB = await dbQueryB.Products.FirstOrDefaultAsync(p => p.Sku == sharedSku);
            Assert.NotNull(productB);
            Assert.Equal("iPhone 15 Pro (Shop B)", productB.Name);
        }
    }

    [Fact]
    public async Task LocationCode_ShouldBeScopedToWarehouse()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var warehouse1 = Guid.NewGuid();
        var warehouse2 = Guid.NewGuid();
        var locationCode = "A-01-01-01";

        using (var db = TestDbContextFactory.Create(tenantId, dbName))
        {
            db.Locations.Add(new Location
            {
                Id = Guid.NewGuid(),
                Code = locationCode,
                WarehouseId = warehouse1,
                TenantId = tenantId
            });

            db.Locations.Add(new Location
            {
                Id = Guid.NewGuid(),
                Code = locationCode,
                WarehouseId = warehouse2,
                TenantId = tenantId
            });

            await db.SaveChangesAsync();
        }

        // Assert: 2 kho khác nhau có thể chứa cùng mã vị trí
        using (var dbQuery = TestDbContextFactory.Create(tenantId, dbName))
        {
            var locations = await dbQuery.Locations.Where(l => l.Code == locationCode).ToListAsync();
            Assert.Equal(2, locations.Count);
            Assert.Contains(locations, l => l.WarehouseId == warehouse1);
            Assert.Contains(locations, l => l.WarehouseId == warehouse2);
        }
    }
}
