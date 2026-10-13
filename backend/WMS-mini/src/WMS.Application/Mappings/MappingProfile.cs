using AutoMapper;
using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Category, CategoryDto>().ReverseMap();
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>();

        CreateMap<Customer, CustomerDto>();
        CreateMap<CreateCustomerDto, Customer>();
        CreateMap<UpdateCustomerDto, Customer>();

        CreateMap<Vendor, VendorDto>();
        CreateMap<CreateVendorDto, Vendor>();
        CreateMap<UpdateVendorDto, Vendor>();

        CreateMap<User, UserProfileDto>()
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse != null ? s.Warehouse.Name : null))
            .ForMember(d => d.WarehouseCode, o => o.MapFrom(s => s.Warehouse != null ? s.Warehouse.Code : null))
            .ForMember(d => d.Roles, o => o.Ignore());

        CreateMap<Product, ProductDto>().ReverseMap();
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();

        CreateMap<Warehouse, WarehouseDto>().ReverseMap();
        CreateMap<CreateWarehouseDto, Warehouse>();
        CreateMap<UpdateWarehouseDto, Warehouse>();

        CreateMap<Location, LocationDto>().ReverseMap();
        CreateMap<CreateLocationDto, Location>();
        CreateMap<UpdateLocationDto, Location>();

        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse != null ? s.Warehouse.Name : null))
            .ForMember(d => d.WarehouseCode, o => o.MapFrom(s => s.Warehouse != null ? s.Warehouse.Code : null))
            .ReverseMap();
        CreateMap<CreatePurchaseOrderDto, PurchaseOrder>();
        CreateMap<PurchaseOrderDetail, PurchaseOrderDetailDto>().ReverseMap();
        CreateMap<CreatePurchaseOrderDetailDto, PurchaseOrderDetail>();
        CreateMap<UpdatePurchaseOrderDto, PurchaseOrder>();
        CreateMap<UpdatePurchaseOrderDetailDto, PurchaseOrderDetail>();
        

        CreateMap<Receiving, ReceivingDto>()
            .ForMember(d => d.PoNumber, o => o.MapFrom(s => s.PurchaseOrder.PoNumber))
            .ForMember(d => d.ReceivedByName, o => o.MapFrom(s => s.ReceivedBy != null ? s.ReceivedBy.UserName : null))
            .ForMember(d => d.Details, o => o.MapFrom(s => s.ReceivingDetails))
            .ForMember(d => d.DiscrepancyReport, o => o.MapFrom(s => s.DiscrepancyReport));
        CreateMap<CreateReceivingDto, Receiving>()
            .ForMember(d => d.ReceivingDetails, o => o.Ignore());
        CreateMap<ReceivingDetail, ReceivingDetailDto>()
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));

        CreateMap<DiscrepancyReport, DiscrepancyReportDto>()
            .ForMember(d => d.ReceivingNo, o => o.MapFrom(s => s.Receiving != null ? s.Receiving.ReceivingNo : string.Empty))
            .ForMember(d => d.PoNumber, o => o.MapFrom(s => s.PurchaseOrder != null ? s.PurchaseOrder.PoNumber : null))
            .ForMember(d => d.VendorName, o => o.MapFrom(s => s.Vendor != null ? s.Vendor.Name : (s.PurchaseOrder != null ? s.PurchaseOrder.VendorName : s.VendorName)))
            .ForMember(d => d.ApprovedByName, o => o.MapFrom(s => s.ApprovedBy != null ? s.ApprovedBy.FullName : null));

        CreateMap<VendorDebtLedger, VendorDebtLedgerDto>()
            .ForMember(d => d.VendorName, o => o.MapFrom(s => s.Vendor != null ? s.Vendor.Name : s.VendorName))
            .ForMember(d => d.SourceReceivingNo, o => o.MapFrom(s => s.SourceReceiving != null ? s.SourceReceiving.ReceivingNo : null))
            .ForMember(d => d.SourcePoNumber, o => o.MapFrom(s => s.SourceReceiving != null && s.SourceReceiving.PurchaseOrder != null ? s.SourceReceiving.PurchaseOrder.PoNumber : null))
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product != null ? s.Product.Sku : null))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : null))
            .ForMember(d => d.ResolvedInPoNumber, o => o.MapFrom(s => s.ResolvedInPo != null ? s.ResolvedInPo.PoNumber : null));

        CreateMap<PutAwayTask, PutAwayTaskDto>()
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.FromLocationCode, o => o.MapFrom(s => s.FromLocation != null ? s.FromLocation.Code : null))
            .ForMember(d => d.ToLocationCode, o => o.MapFrom(s => s.ToLocation != null ? s.ToLocation.Code : null))
            .ForMember(d => d.WarehouseId, o => o.MapFrom(s => s.FromLocation != null ? s.FromLocation.WarehouseId : (s.ToLocation != null ? s.ToLocation.WarehouseId : (Guid?)null)))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.FromLocation != null ? s.FromLocation.Warehouse.Name : (s.ToLocation != null ? s.ToLocation.Warehouse.Name : null)))
            .ForMember(d => d.AssignToName, o => o.MapFrom(s => s.AssignTo != null ? s.AssignTo.FullName : null))
            .ForMember(d => d.AssignToAvatarUrl, o => o.MapFrom(s => s.AssignTo != null ? s.AssignTo.AvatarUrl : null));
        CreateMap<CreatePutAwayTaskDto, PutAwayTask>();
        CreateMap<UpdatePutAwayTaskDto, PutAwayTask>()
            // Assign chỉ qua endpoint /assign — update (đặt vị trí) không được phép đụng AssignToId
            .ForMember(d => d.AssignToId, o => o.Ignore());

        CreateMap<Stock, StockDto>()
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.LocationCode, o => o.MapFrom(s => s.Location.Code))
            .ForMember(d => d.WarehouseId, o => o.MapFrom(s => s.Location.WarehouseId))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Location.Warehouse != null ? s.Location.Warehouse.Name : string.Empty));
        CreateMap<StockMovement, StockMovementDto>()
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.LocationCode, o => o.MapFrom(s => s.Location.Code))
            .ForMember(d => d.WarehouseId, o => o.MapFrom(s => s.Location.WarehouseId))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Location.Warehouse != null ? s.Location.Warehouse.Name : null))
            .ForMember(d => d.OccurredAtUtc, o => o.MapFrom(s => s.CreatedDate))
            .ForMember(d => d.ActorUserId, o => o.MapFrom(s => s.CreatedById))
            .ForMember(d => d.ActorDisplayName, o => o.MapFrom(s => s.CreatedBy != null ? s.CreatedBy.FullName : null))
            .ForMember(d => d.ActorAvatarUrl, o => o.MapFrom(s => s.CreatedBy != null ? s.CreatedBy.AvatarUrl : null));

        CreateMap<StockAdjustment, StockAdjustmentDto>();
        CreateMap<CreateStockAdjustmentDto, StockAdjustment>()
            .ForMember(d => d.Details, o => o.Ignore());
        CreateMap<StockAdjustmentDetail, StockAdjustmentDetailDto>()
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.LocationCode, o => o.MapFrom(s => s.Location.Code))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Location.Warehouse != null ? s.Location.Warehouse.Name : string.Empty));

        CreateMap<SaleOrder, SaleOrderDto>()
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse != null ? s.Warehouse.Name : null))
            .ForMember(d => d.WarehouseCode, o => o.MapFrom(s => s.Warehouse != null ? s.Warehouse.Code : null))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer != null ? s.Customer.Name : s.CustomerName));
        CreateMap<CreateSaleOrderDto, SaleOrder>();
        CreateMap<CreateSaleOrderDetailDto, SaleOrderDetail>();
        CreateMap<SaleOrderDetail, SaleOrderDetailDto>()
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));

        CreateMap<Picking, PickingDto>()
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.AssignedToName, o => o.MapFrom(s => s.AssignedTo != null ? s.AssignedTo.UserName : null))
            .ForMember(d => d.AssignedToAvatarUrl, o => o.MapFrom(s => s.AssignedTo != null ? s.AssignedTo.AvatarUrl : null))
            .ForMember(d => d.Details, o => o.MapFrom(s => s.PickingDetails));
        CreateMap<CreatePickingDto, Picking>();
        CreateMap<PickingDetail, PickingDetailDto>()
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.LocationCode, o => o.MapFrom(s => s.Location != null ? s.Location.Code : null));

        CreateMap<Shipment, ShipmentDto>()
            .ForMember(d => d.SaleOrderNo, o => o.MapFrom(s => s.SaleOrder.OrderNo));
        CreateMap<CreateShipmentDto, Shipment>();

        CreateMap<BackorderLink, BackorderLinkDto>()
            .ForMember(d => d.SaleOrderNo, o => o.MapFrom(s => s.SaleOrderDetail.SaleOrder.OrderNo))
            .ForMember(d => d.SaleOrderId, o => o.MapFrom(s => s.SaleOrderDetail.SaleOrderId))
            .ForMember(d => d.PurchaseOrderNo, o => o.MapFrom(s => s.PurchaseOrderDetail.PurchaseOrder.PoNumber))
            .ForMember(d => d.PurchaseOrderId, o => o.MapFrom(s => s.PurchaseOrderDetail.PurchaseOrderId))
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.SaleOrderDetail.ProductId))
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.SaleOrderDetail.Product.Sku))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.SaleOrderDetail.Product.Name));


        CreateMap<AuditLog, AuditLogDto>()
            .ForMember(d => d.ActorDisplayName, o => o.MapFrom(s => s.ActorUser!.FullName))
            .ForMember(d => d.ActorAvatarUrl, o => o.MapFrom(s => s.ActorUser != null ? s.ActorUser.AvatarUrl : null));
        CreateMap<StatusHistory, StatusHistoryDto>()
            .ForMember(d => d.ActorDisplayName, o => o.MapFrom(s => s.ActorUser!.FullName))
            .ForMember(d => d.ActorAvatarUrl, o => o.MapFrom(s => s.ActorUser != null ? s.ActorUser.AvatarUrl : null));
    }
}
