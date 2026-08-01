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

        CreateMap<Product, ProductDto>().ReverseMap();
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();

        CreateMap<Warehouse, WarehouseDto>().ReverseMap();
        CreateMap<CreateWarehouseDto, Warehouse>();
        CreateMap<UpdateWarehouseDto, Warehouse>();

        CreateMap<CreateWarehouseDto, Warehouse>();
        CreateMap<UpdateWarehouseDto, Warehouse>();

        CreateMap<Location, LocationDto>().ReverseMap();
        CreateMap<CreateLocationDto, Location>();
        CreateMap<UpdateLocationDto, Location>();

        CreateMap<PurchaseOrder, PurchaseOrderDto>().ReverseMap();
        CreateMap<CreatePurchaseOrderDto, PurchaseOrder>();
        CreateMap<PurchaseOrderDetail, PurchaseOrderDetailDto>().ReverseMap();
        CreateMap<CreatePurchaseOrderDetailDto, PurchaseOrderDetail>();

        CreateMap<Receiving, ReceivingDto>()
            .ForMember(d => d.PoNumber, o => o.MapFrom(s => s.PurchaseOrder.PoNumber))
            .ForMember(d => d.ReceivedByName, o => o.MapFrom(s => s.ReceivedBy != null ? s.ReceivedBy.UserName : null));
        CreateMap<CreateReceivingDto, Receiving>()
            .ForMember(d => d.ReceivingDetails, o => o.Ignore());
        CreateMap<ReceivingDetail, ReceivingDetailDto>()
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));

        CreateMap<PutAwayTask, PutAwayTaskDto>()
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.FromLocationCode, o => o.MapFrom(s => s.FromLocation != null ? s.FromLocation.Code : null))
            .ForMember(d => d.ToLocationCode, o => o.MapFrom(s => s.ToLocation != null ? s.ToLocation.Code : null))
            .ForMember(d => d.AssignToName, o => o.MapFrom(s => s.AssignTo != null ? s.AssignTo.UserName : null));
        CreateMap<CreatePutAwayTaskDto, PutAwayTask>();
        CreateMap<UpdatePutAwayTaskDto, PutAwayTask>();

        CreateMap<Stock, StockDto>().ReverseMap();
        CreateMap<StockMovement, StockMovementDto>().ReverseMap();

        CreateMap<SaleOrder, SaleOrderDto>().ReverseMap();
        CreateMap<CreateSaleOrderDto, SaleOrder>();
        CreateMap<SaleOrderDetail, SaleOrderDetailDto>().ReverseMap();

        CreateMap<Picking, PickingDto>().ReverseMap();
        CreateMap<CreatePickingDto, Picking>();
        CreateMap<PickingDetail, PickingDetailDto>().ReverseMap();

        CreateMap<Shipment, ShipmentDto>().ReverseMap();

        CreateMap<Rma, RmaDto>().ReverseMap();
        CreateMap<CreateRmaDto, Rma>();
        CreateMap<RmaDetail, RmaDetailDto>().ReverseMap();

        CreateMap<AssociationRule, AssociationRuleDto>().ReverseMap();
    }
}