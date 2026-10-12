using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Application.Mappings;
using WMS.Application.Services;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Repositories;
using WMS.Infrastructure.Services;
using WMS.UnitTests.Common;

namespace WMS.UnitTests.Inbound;

public class DiscrepancyQuarantineTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    private static (ReceivingService receivingService, WmsDbContext db, Guid tenantId, Guid userId, Mock<ICurrentUserService> currentUserMock)
        CreateTestContext(string? userRole = "WarehouseManager")
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId, Guid.NewGuid().ToString());
        var mapper = CreateMapper();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(tenantId);
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.UserName).Returns("manager_tester");
        currentUserMock.Setup(u => u.IsInRole(It.IsAny<string[]>()))
            .Returns<string[]>(roles => userRole != null && (roles.Contains(userRole) || userRole == "All"));

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() => db.SaveChangesAsync());
        uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        var receivingRepo = new SqlReceivingRepository(db);
        var putAwayRepo = new SqlPutAwayTaskRepository(db);
        var poRepo = new SqlPurchaseOrderRepository(db);
        var locationRepo = new SqlLocationRepository(db);

        var receivingService = new ReceivingService(
            receivingRepo,
            putAwayRepo,
            poRepo,
            locationRepo,
            uowMock.Object,
            currentUserMock.Object,
            mapper);

        return (receivingService, db, tenantId, userId, currentUserMock);
    }

    private static async Task<(PurchaseOrder po, Product product, Warehouse warehouse, Location storageLoc, Location quarantineLoc)>
        SeedInboundEnvironmentAsync(WmsDbContext db, Guid tenantId, bool includeQuarantine = true)
    {
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Kho Tổng Tân Bình",
            Code = "WH-TB",
            TenantId = tenantId
        };
        db.Warehouses.Add(warehouse);

        var storageLoc = new Location
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouse.Id,
            Code = "LOC-STORAGE-01",
            LocationType = LocationType.Storage,
            MaxQuantity = 1000,
            TenantId = tenantId
        };
        db.Locations.Add(storageLoc);

        Location? quarantineLoc = null;
        if (includeQuarantine)
        {
            quarantineLoc = new Location
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouse.Id,
                Code = "LOC-QUARANTINE-01",
                LocationType = LocationType.Quarantine,
                MaxQuantity = 500,
                TenantId = tenantId
            };
            db.Locations.Add(quarantineLoc);
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = "SKU-TEST-001",
            Name = "Màn hình Dell 24 inch",
            Price = 3500000m,
            TenantId = tenantId
        };
        db.Products.Add(product);

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = "PO-2026-0001",
            VendorName = "Dell Vietnam",
            WarehouseId = warehouse.Id,
            Status = PurchaseOrderStatus.Approved,
            TotalAmount = 35000000m,
            TenantId = tenantId,
            PurchaseOrderDetails = new List<PurchaseOrderDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    OrderedQuantity = 10,
                    ReceivedQuantity = 0,
                    UnitPrice = 3500000m,
                    TenantId = tenantId
                }
            }
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        return (po, product, warehouse, storageLoc, quarantineLoc!);
    }

    [Fact]
    public async Task Confirm_DamagedWithQuarantineAndHold_GeneratesQuarantinePutAwayTask_AndPendingDiscrepancyReport()
    {
        // Arrange
        var (receivingService, db, tenantId, _, _) = CreateTestContext("WarehouseManager");
        var (po, product, _, _, quarantineLoc) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: true);

        var created = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            InvoiceNumber = "INV-001",
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 5,
                    Condition = ProductCondition.Ok,
                    UnitPrice = 3500000m
                },
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 3,
                    Condition = ProductCondition.Damaged,
                    UnitPrice = 3500000m,
                    DiscrepancyAction = DiscrepancyAction.QuarantineAndHold
                }
            }
        });

        // Act
        var confirmed = await receivingService.ConfirmAsync(created.Id);

        // Assert
        Assert.NotNull(confirmed);
        Assert.True(confirmed.HasDiscrepancy);
        Assert.NotNull(confirmed.DiscrepancyReport);
        Assert.Equal(DiscrepancyReportStatus.Pending, confirmed.DiscrepancyReport.Status);

        var tasks = await new SqlPutAwayTaskRepository(db).GetAllAsync();
        Assert.Equal(2, tasks.Count);

        var okTask = tasks.FirstOrDefault(t => t.Quantity == 5);
        Assert.NotNull(okTask);
        Assert.Null(okTask.ToLocationId);

        var damagedTask = tasks.FirstOrDefault(t => t.Quantity == 3);
        Assert.NotNull(damagedTask);
        Assert.Equal(quarantineLoc.Id, damagedTask.ToLocationId);
    }

    [Fact]
    public async Task Confirm_DamagedWithReturnToVendor_DoesNotGeneratePutAwayTask_CreatesPendingDiscrepancyReport()
    {
        // Arrange
        var (receivingService, db, tenantId, _, _) = CreateTestContext("WarehouseManager");
        var (po, product, _, _, _) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: true);

        var created = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 2,
                    Condition = ProductCondition.Damaged,
                    UnitPrice = 3500000m,
                    DiscrepancyAction = DiscrepancyAction.ReturnToVendor
                }
            }
        });

        // Act
        var confirmed = await receivingService.ConfirmAsync(created.Id);

        // Assert
        Assert.NotNull(confirmed);
        Assert.True(confirmed.HasDiscrepancy);
        Assert.NotNull(confirmed.DiscrepancyReport);
        Assert.Equal(DiscrepancyReportStatus.Pending, confirmed.DiscrepancyReport.Status);

        var tasks = await new SqlPutAwayTaskRepository(db).GetAllAsync();
        Assert.Empty(tasks); // No PutAway task generated for ReturnToVendor
    }

    [Fact]
    public async Task Confirm_DamagedWithQuarantineAndHold_ThrowsWhenWarehouseHasNoQuarantineLocation()
    {
        // Arrange
        var (receivingService, db, tenantId, _, _) = CreateTestContext("WarehouseManager");
        var (po, product, _, _, _) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: false);

        var created = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 2,
                    Condition = ProductCondition.Damaged,
                    UnitPrice = 3500000m,
                    DiscrepancyAction = DiscrepancyAction.QuarantineAndHold
                }
            }
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => receivingService.ConfirmAsync(created.Id));
        Assert.Contains("Quarantine", ex.Message);
    }

    [Fact]
    public async Task Picking_GetAvailableByProductAndWarehouse_ExcludesQuarantineLocation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(tenantId, Guid.NewGuid().ToString());
        var (po, product, warehouse, storageLoc, quarantineLoc) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: true);

        // Add 10 items to Quarantine location
        db.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            LocationId = quarantineLoc.Id,
            OnhandQty = 10,
            ReservedQty = 0,
            TenantId = tenantId
        });

        // Add 4 items to normal Storage location
        db.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            LocationId = storageLoc.Id,
            OnhandQty = 4,
            ReservedQty = 0,
            TenantId = tenantId
        });
        await db.SaveChangesAsync();

        var stockRepo = new SqlStockRepository(db);

        // Act
        var availableStocks = await stockRepo.GetAvailableByProductAndWarehouseAsync(product.Id, warehouse.Id);

        // Assert
        Assert.Single(availableStocks);
        Assert.Equal(storageLoc.Id, availableStocks[0].LocationId);
        Assert.Equal(4, availableStocks[0].OnhandQty);
        Assert.DoesNotContain(availableStocks, s => s.LocationId == quarantineLoc.Id);
    }

    [Fact]
    public void ValidatePutAwayEligibility_SymmetricConstraints_OkVersusQuarantine()
    {
        // Arrange
        var confirmedReceiving = new Receiving { Status = ReceivingStatus.Confirmed };

        var okDetail = new ReceivingDetail
        {
            Receiving = confirmedReceiving,
            Condition = ProductCondition.Ok
        };

        var damagedDetail = new ReceivingDetail
        {
            Receiving = confirmedReceiving,
            Condition = ProductCondition.Damaged
        };

        var storageLocation = new Location { LocationType = LocationType.Storage };
        var quarantineLocation = new Location { LocationType = LocationType.Quarantine };

        // Ok goods cannot go to Quarantine
        var okToQuarantineEx = Assert.Throws<InvalidOperationException>(() =>
            PutAwayService.ValidatePutAwayEligibility(okDetail, quarantineLocation));
        Assert.Contains("Quarantine", okToQuarantineEx.Message);

        // Ok goods can go to Storage
        PutAwayService.ValidatePutAwayEligibility(okDetail, storageLocation); // should not throw

        // Damaged goods cannot go to Storage
        var damagedToStorageEx = Assert.Throws<InvalidOperationException>(() =>
            PutAwayService.ValidatePutAwayEligibility(damagedDetail, storageLocation));
        Assert.Contains("Quarantine", damagedToStorageEx.Message);

        // Damaged goods can go to Quarantine
        PutAwayService.ValidatePutAwayEligibility(damagedDetail, quarantineLocation); // should not throw
    }

    [Fact]
    public async Task ApproveDiscrepancyReport_WarehouseManager_ApprovesSuccessfully()
    {
        // Arrange
        var (receivingService, db, tenantId, userId, _) = CreateTestContext("WarehouseManager");
        var (po, product, _, _, _) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: true);

        var created = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 3,
                    Condition = ProductCondition.Damaged,
                    UnitPrice = 3500000m,
                    DiscrepancyAction = DiscrepancyAction.QuarantineAndHold
                }
            }
        });
        await receivingService.ConfirmAsync(created.Id);

        // Act
        var approved = await receivingService.ApproveDiscrepancyReportAsync(created.Id, new ApproveDiscrepancyReportDto
        {
            IsApproved = true,
            Notes = "Đã đối chiếu với tài xế, xác nhận cất cách ly chờ đổi."
        });

        // Assert
        Assert.Equal(DiscrepancyReportStatus.Approved, approved.Status);
        Assert.Equal(userId, approved.ApprovedById);
        Assert.NotNull(approved.ApprovedDate);
        Assert.Contains("Đã đối chiếu với tài xế", approved.ResolutionSummary);
    }

    [Fact]
    public async Task ApproveDiscrepancyReport_NonManager_ThrowsUnauthorizedAccessException()
    {
        // Arrange: User is only WarehouseStaff
        var (receivingService, db, tenantId, _, _) = CreateTestContext("WarehouseStaff");
        var (po, product, _, _, _) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: true);

        var created = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 3,
                    Condition = ProductCondition.Damaged,
                    UnitPrice = 3500000m,
                    DiscrepancyAction = DiscrepancyAction.QuarantineAndHold
                }
            }
        });
        await receivingService.ConfirmAsync(created.Id);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            receivingService.ApproveDiscrepancyReportAsync(created.Id, new ApproveDiscrepancyReportDto
            {
                IsApproved = true,
                Notes = "Nhân viên không có quyền duyệt"
            }));
    }

    [Fact]
    public async Task Confirm_PerfectReceiving_NoDiscrepancy_NoReport()
    {
        // Arrange
        var (receivingService, db, tenantId, _, _) = CreateTestContext("WarehouseManager");
        var (po, product, _, _, _) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: true);

        var created = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            InvoiceTotalAmount = 35000000m,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 10,
                    Condition = ProductCondition.Ok,
                    UnitPrice = 3500000m
                }
            }
        });

        // Act
        var confirmed = await receivingService.ConfirmAsync(created.Id);

        // Assert
        Assert.NotNull(confirmed);
        Assert.False(confirmed.HasDiscrepancy);
        Assert.Null(confirmed.DiscrepancyReport);

        var reportInDb = await new SqlReceivingRepository(db).GetDiscrepancyReportByReceivingIdAsync(created.Id);
        Assert.Null(reportInDb);

        var tasks = await new SqlPutAwayTaskRepository(db).GetAllAsync();
        Assert.Single(tasks);
        Assert.Null(tasks[0].ToLocationId);
    }

    [Fact]
    public async Task GetDiscrepancyReport_WhenExists_ReturnsMappedDto()
    {
        // Arrange
        var (receivingService, db, tenantId, _, _) = CreateTestContext("WarehouseManager");
        var (po, product, _, _, _) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: true);

        var created = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 5,
                    Condition = ProductCondition.Damaged,
                    UnitPrice = 3500000m,
                    DiscrepancyAction = DiscrepancyAction.QuarantineAndHold
                }
            }
        });
        await receivingService.ConfirmAsync(created.Id);

        // Act
        var reportDto = await receivingService.GetDiscrepancyReportAsync(created.Id);

        // Assert
        Assert.NotNull(reportDto);
        Assert.Equal(created.Id, reportDto.ReceivingId);
        Assert.Equal(DiscrepancyReportStatus.Pending, reportDto.Status);
        Assert.NotNull(reportDto.ResolutionSummary);
    }

    [Fact]
    public async Task ApproveDiscrepancyReport_WhenRejected_SetsRejectedStatusAndBlocksReapproval()
    {
        // Arrange
        var (receivingService, db, tenantId, userId, _) = CreateTestContext("WarehouseManager");
        var (po, product, _, _, _) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: true);

        var created = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 5,
                    Condition = ProductCondition.Damaged,
                    UnitPrice = 3500000m,
                    DiscrepancyAction = DiscrepancyAction.QuarantineAndHold
                }
            }
        });
        await receivingService.ConfirmAsync(created.Id);

        // Act: Reject report
        var rejected = await receivingService.ApproveDiscrepancyReportAsync(created.Id, new ApproveDiscrepancyReportDto
        {
            IsApproved = false,
            Notes = "Không chấp nhận giải trình từ nhà cung cấp."
        });

        // Assert
        Assert.Equal(DiscrepancyReportStatus.Rejected, rejected.Status);
        Assert.Equal(userId, rejected.ApprovedById);

        // Act & Assert: Attempting to approve a non-pending report throws
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            receivingService.ApproveDiscrepancyReportAsync(created.Id, new ApproveDiscrepancyReportDto
            {
                IsApproved = true,
                Notes = "Cố gắng duyệt lại"
            }));
        Assert.Contains("đã được xử lý", ex.Message);
    }

    [Fact]
    public async Task ConfirmAsync_WhenReturnToVendor_DoesNotIncrementPoReceivedQuantity()
    {
        // Arrange: 10 ordered. 6 received OK, 4 Damaged returned to vendor.
        var (receivingService, db, tenantId, _, _) = CreateTestContext("WarehouseManager");
        var (po, product, _, _, _) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: true);

        var created = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 6,
                    Condition = ProductCondition.Ok,
                    UnitPrice = 3500000m
                },
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 0,
                    ActualQuantity = 4,
                    Condition = ProductCondition.Damaged,
                    UnitPrice = 3500000m,
                    DiscrepancyAction = DiscrepancyAction.ReturnToVendor
                }
            }
        });

        // Act
        await receivingService.ConfirmAsync(created.Id);

        // Assert: PO received quantity should only be 6 (the OK items), not 10.
        var updatedPo = await db.PurchaseOrders.Include(p => p.PurchaseOrderDetails).FirstAsync(p => p.Id == po.Id);
        var poDetail = updatedPo.PurchaseOrderDetails.First(d => d.ProductId == product.Id);
        Assert.Equal(6, poDetail.ReceivedQuantity);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, updatedPo.Status);

        // Only 1 PutAwayTask created (for the 6 OK items)
        var putAwayTasks = await new SqlPutAwayTaskRepository(db).GetAllAsync();
        Assert.Single(putAwayTasks);
        Assert.Equal(6, putAwayTasks[0].Quantity);
    }

    [Fact]
    public async Task CreateAsync_WithZeroActualQuantity_WithoutAction_Throws_WithAction_Succeeds()
    {
        // Arrange
        var (receivingService, db, tenantId, _, _) = CreateTestContext("WarehouseManager");
        var (po, product, _, _, _) = await SeedInboundEnvironmentAsync(db, tenantId, includeQuarantine: true);

        // Act & Assert 1: Zero quantity with condition Ok and no action -> throws
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 0,
                    Condition = ProductCondition.Ok
                }
            }
        }));
        Assert.Contains("xử lý thiếu", ex.Message);

        // Act & Assert 2: Zero quantity with BacklogDeliverLater action -> succeeds
        var validCreated = await receivingService.CreateAsync(new CreateReceivingDto
        {
            PurchaseOrderId = po.Id,
            Details = new List<CreateReceivingDetailDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ExpectedQuantity = 10,
                    ActualQuantity = 0,
                    Condition = ProductCondition.Missing,
                    DiscrepancyAction = DiscrepancyAction.BacklogDeliverLater
                }
            }
        });
        Assert.NotNull(validCreated);
        Assert.Single(validCreated.Details);
        Assert.Equal(0, validCreated.Details[0].ActualQuantity);
        Assert.Equal(DiscrepancyAction.BacklogDeliverLater, validCreated.Details[0].DiscrepancyAction);
    }

    [Fact]
    public void ValidatePutAwayEligibility_WhenReturnToVendor_Throws()
    {
        // Arrange
        var confirmedReceiving = new Receiving { Status = ReceivingStatus.Confirmed };
        var rtvDetail = new ReceivingDetail
        {
            Receiving = confirmedReceiving,
            Condition = ProductCondition.Damaged,
            DiscrepancyAction = DiscrepancyAction.ReturnToVendor
        };

        var quarantineLocation = new Location { LocationType = LocationType.Quarantine };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PutAwayService.ValidatePutAwayEligibility(rtvDetail, quarantineLocation));
        Assert.Contains("trả nhà cung cấp", ex.Message);
    }
}
