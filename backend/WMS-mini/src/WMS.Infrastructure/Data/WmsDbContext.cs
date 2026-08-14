using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Data;

public class WmsDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options) { }

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails => Set<PurchaseOrderDetail>();
    public DbSet<Receiving> Receivings => Set<Receiving>();
    public DbSet<ReceivingDetail> ReceivingDetails => Set<ReceivingDetail>();
    public DbSet<PutAwayTask> PutAwayTasks => Set<PutAwayTask>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<SaleOrder> SaleOrders => Set<SaleOrder>();
    public DbSet<SaleOrderDetail> SaleOrderDetails => Set<SaleOrderDetail>();
    public DbSet<Picking> Pickings => Set<Picking>();
    public DbSet<PickingDetail> PickingDetails => Set<PickingDetail>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Rma> Rmas => Set<Rma>();
    public DbSet<RmaDetail> RmaDetails => Set<RmaDetail>();
    public DbSet<AssociationRule> AssociationRules => Set<AssociationRule>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentDetail> StockAdjustmentDetails => Set<StockAdjustmentDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Delete all cascade paths — SQL Server doesn't allow multiple cascades
        foreach (var relationship in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.NoAction;
        }

        modelBuilder.Entity<Shipment>()
            .HasOne(s => s.SaleOrder)
            .WithOne(o => o.Shipment)
            .HasForeignKey<Shipment>(s => s.SaleOrderId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Shipment>()
            .HasIndex(s => s.SaleOrderId)
            .IsUnique();

        modelBuilder.Entity<Stock>()
            .HasIndex(s => new { s.ProductId, s.LocationId })
            .IsUnique();

        modelBuilder.Entity<Warehouse>()
            .HasIndex(w => w.Code)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(p => p.PoNumber)
            .IsUnique();

        modelBuilder.Entity<SaleOrder>()
            .HasIndex(s => s.OrderNo)
            .IsUnique();

        modelBuilder.Entity<StockAdjustment>()
            .HasIndex(a => a.AdjustmentNo)
            .IsUnique();

        modelBuilder.Entity<Picking>()
            .HasIndex(p => p.PickingNo)
            .IsUnique();
    }
}
