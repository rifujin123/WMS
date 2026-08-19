using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WMS.Application.Interfaces;
using WMS.Domain.Common;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Data;

public class WmsDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    private static readonly HashSet<string> ExcludedAuditProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedBy", "UpdatedBy", "DeletedBy", "PasswordHash", "Password", "SecurityStamp",
        "ConcurrencyStamp", "NormalizedUserName", "NormalizedEmail", "RefreshToken"
    };

    private readonly ICurrentUserService? _currentUserService;

    public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options)
    {
    }

    public WmsDbContext(DbContextOptions<WmsDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();

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

        modelBuilder.Entity<PurchaseOrderDetail>()
            .Property(d => d.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<SaleOrder>()
            .HasIndex(s => s.OrderNo)
            .IsUnique();

        modelBuilder.Entity<Receiving>()
            .HasIndex(r => r.ReceivingNo)
            .IsUnique();

        modelBuilder.Entity<Receiving>()
            .HasIndex(r => r.PurchaseOrderId)
            .IsUnique()
            .HasFilter("[Status] = 1");

        modelBuilder.Entity<StockAdjustment>()
            .HasIndex(a => a.AdjustmentNo)
            .IsUnique();

        modelBuilder.Entity<Picking>()
            .HasIndex(p => p.PickingNo)
            .IsUnique();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(entityType => typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeleted = Expression.Property(parameter, nameof(BaseAuditableEntity.IsDeleted));
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(Expression.Not(isDeleted), parameter));
        }

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(a => a.ActorUser)
                .WithMany()
                .HasForeignKey(a => a.ActorUserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(a => new { a.EntityType, a.EntityId, a.OccurredAtUtc });
            entity.HasIndex(a => new { a.ActorUserId, a.OccurredAtUtc });
        });

        modelBuilder.Entity<StatusHistory>(entity =>
        {
            entity.HasOne(s => s.ActorUser)
                .WithMany()
                .HasForeignKey(s => s.ActorUserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(s => new { s.EntityType, s.EntityId, s.OccurredAtUtc });
            entity.HasIndex(s => new { s.ActorUserId, s.OccurredAtUtc });
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareAuditEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareAuditEntries();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareAuditEntries()
    {
        ChangeTracker.DetectChanges();
        var now = DateTime.UtcNow;
        var actorId = _currentUserService?.UserId;
        var auditLogs = new List<AuditLog>();
        var statusHistories = new List<StatusHistory>();

        foreach (var entry in ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(entry => entry.Entity is not AuditLog && entry.Entity is not StatusHistory))
        {
            var isAuditable = entry.Entity is BaseAuditableEntity;
            if (!isAuditable && entry.Entity is not User)
            {
                continue;
            }

            if (entry.Property("Id").CurrentValue is not Guid entityId || entityId == Guid.Empty)
            {
                if (entry.Entity is not BaseAuditableEntity auditableEntity || entry.State != EntityState.Added)
                {
                    continue;
                }

                auditableEntity.Id = Guid.NewGuid();
                entityId = auditableEntity.Id;
            }

            var initialState = entry.State;
            var changedFields = GetChangedFields(entry, initialState);
            var originalValues = SerializeValues(entry, false);

            if (isAuditable && entry.Entity is BaseAuditableEntity auditable)
            {
                switch (initialState)
                {
                    case EntityState.Added:
                        auditable.CreatedDate = now;
                        auditable.CreatedById = actorId;
                        auditable.IsDeleted = false;
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedDate = now;
                        auditable.UpdatedById = actorId;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        auditable.IsDeleted = true;
                        auditable.DeletedById = actorId;
                        auditable.DeletedDate = now;
                        break;
                }
            }

            var action = initialState switch
            {
                EntityState.Added => "Created",
                EntityState.Deleted => "Deleted",
                EntityState.Modified when changedFields.Length > 0 => "Updated",
                _ => null
            };

            if (action is not null)
            {
                auditLogs.Add(new AuditLog(
                    Guid.NewGuid(), entry.Metadata.ClrType.Name, entityId, action, actorId, now,
                    originalValues, SerializeValues(entry, true), JsonSerializer.Serialize(changedFields)));
            }

            if (initialState == EntityState.Modified)
            {
                var statusProperty = entry.Properties.FirstOrDefault(property =>
                    string.Equals(property.Metadata.Name, "Status", StringComparison.Ordinal));
                if (statusProperty is not null && !Equals(statusProperty.OriginalValue, statusProperty.CurrentValue))
                {
                    statusHistories.Add(new StatusHistory(
                        Guid.NewGuid(),
                        entry.Metadata.ClrType.Name,
                        entityId,
                        SerializeStatus(statusProperty.OriginalValue),
                        SerializeStatus(statusProperty.CurrentValue),
                        "StatusChanged",
                        actorId,
                        now));
                }
            }
        }

        AuditLogs.AddRange(auditLogs);
        StatusHistories.AddRange(statusHistories);
    }

    private static string[] GetChangedFields(EntityEntry entry, EntityState state)
    {
        if (state == EntityState.Added)
        {
            return entry.Properties
                .Select(property => property.Metadata.Name)
                .Where(name => !ExcludedAuditProperties.Contains(name))
                .ToArray();
        }

        if (state == EntityState.Deleted)
        {
            return new[] { nameof(BaseAuditableEntity.IsDeleted) };
        }

        return entry.Properties
            .Where(property => property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
            .Select(property => property.Metadata.Name)
            .Where(name => !ExcludedAuditProperties.Contains(name))
            .ToArray();
    }

    private static string SerializeStatus(object? value) => value switch
    {
        null => string.Empty,
        Enum status => status.ToString(),
        _ => value.ToString() ?? string.Empty
    };

    private static string? SerializeValues(EntityEntry entry, bool current)
    {
        var values = entry.Properties
            .Where(property => !ExcludedAuditProperties.Contains(property.Metadata.Name))
            .ToDictionary(
                property => property.Metadata.Name,
                property => current ? property.CurrentValue : property.OriginalValue);

        return values.Count == 0 ? null : JsonSerializer.Serialize(values);
    }
}
