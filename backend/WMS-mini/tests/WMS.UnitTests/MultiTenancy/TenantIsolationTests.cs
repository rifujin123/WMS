using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;
using WMS.UnitTests.Common;

namespace WMS.UnitTests.MultiTenancy;

public class TenantIsolationTests
{
    [Fact]
    public async Task QueryFilter_ShouldIsolateProductsBetweenTenants()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Seed with Tenant A
        using (var dbA = TestDbContextFactory.Create(tenantA, dbName))
        {
            dbA.Products.Add(new Product { Id = Guid.NewGuid(), Name = "Sản phẩm A", Sku = "SKU-A", TenantId = tenantA });
            await dbA.SaveChangesAsync();
        }

        // Seed with Tenant B
        using (var dbB = TestDbContextFactory.Create(tenantB, dbName))
        {
            dbB.Products.Add(new Product { Id = Guid.NewGuid(), Name = "Sản phẩm B", Sku = "SKU-B", TenantId = tenantB });
            await dbB.SaveChangesAsync();
        }

        // Act & Assert: Tenant A chỉ thấy sản phẩm của A
        using (var dbCheckA = TestDbContextFactory.Create(tenantA, dbName))
        {
            var productsA = await dbCheckA.Products.ToListAsync();
            Assert.Single(productsA);
            Assert.Equal("SKU-A", productsA[0].Sku);
            Assert.Equal(tenantA, productsA[0].TenantId);
        }

        // Act & Assert: Tenant B chỉ thấy sản phẩm của B
        using (var dbCheckB = TestDbContextFactory.Create(tenantB, dbName))
        {
            var productsB = await dbCheckB.Products.ToListAsync();
            Assert.Single(productsB);
            Assert.Equal("SKU-B", productsB[0].Sku);
            Assert.Equal(tenantB, productsB[0].TenantId);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldAutoAssignCurrentTenantId_WhenNotSpecified()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();

        using var db = TestDbContextFactory.Create(tenantId, dbName);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Sản phẩm Tự gán Tenant",
            Sku = "AUTO-TENANT-01",
            TenantId = Guid.Empty // Chưa gán thủ công
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Assert
        Assert.Equal(tenantId, product.TenantId);
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldReturnAllTenantsData()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        using (var dbSeed = TestDbContextFactory.Create(tenantA, dbName))
        {
            dbSeed.Tenants.Add(new Tenant { Id = tenantA, Name = "Tenant A", Code = "tenant-a", IsActive = true });
            dbSeed.Tenants.Add(new Tenant { Id = tenantB, Name = "Tenant B", Code = "tenant-b", IsActive = true });
            await dbSeed.SaveChangesAsync();
        }

        // Act
        using (var dbQuery = TestDbContextFactory.Create(tenantA, dbName))
        {
            var allTenants = await dbQuery.Tenants.IgnoreQueryFilters().ToListAsync();

            // Assert
            Assert.True(allTenants.Count >= 2);
            Assert.Contains(allTenants, t => t.Id == tenantA);
            Assert.Contains(allTenants, t => t.Id == tenantB);
        }
    }
}
