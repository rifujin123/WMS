using AutoMapper;
using WMS.Domain.Entities;
using WMS.Application.DTOs;

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

        CreateMap<Receiving, ReceivingDto>().ReverseMap();
        CreateMap<CreateReceivingDto, Receiving>();
        CreateMap<ReceivingDetail, ReceivingDetailDto>().ReverseMap();
        CreateMap<CreateReceivingDetailDto, ReceivingDetail>();

        CreateMap<PutAwayTask, PutAwayTaskDto>().ReverseMap();

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
