using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services;

public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _repo;
    private readonly ISaleOrderRepository _saleOrderRepo;
    private readonly IShipmentGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ShipmentService(
        IShipmentRepository repo,
        ISaleOrderRepository saleOrderRepo,
        IShipmentGateway gateway,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repo = repo;
        _saleOrderRepo = saleOrderRepo;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<ShipmentDto>> GetAllAsync()
    {
        return _mapper.Map<List<ShipmentDto>>(await _repo.GetAllAsync());
    }

    public async Task<ShipmentDto?> GetByIdAsync(Guid id)
    {
        var shipment = await _repo.GetByIdAsync(id);
        if(shipment == null)
            return null;
        return _mapper.Map<ShipmentDto>(shipment);
    }

    public async Task<ShipmentDto?> GetBySaleOrderAsync(Guid saleOrderId)
    {
        var shipment = await _repo.GetBySaleOrderIdAsync(saleOrderId);
        if(shipment == null)
            return null;
        return _mapper.Map<ShipmentDto>(shipment);
    }

    public async Task<ShipmentDto> CreateAsync(CreateShipmentDto dto)
    {
        var saleOrder = await _saleOrderRepo.GetByIdAsync(dto.SaleOrderId);
        if (saleOrder == null)
            throw new InvalidOperationException("SaleOrder not found.");

        if (saleOrder.Status != SaleOrderStatus.Packed)
            throw new InvalidOperationException(
                $"Cannot create shipment for SaleOrder in '{saleOrder.Status}' status. Must be 'Packed'.");

        if (await _repo.GetBySaleOrderIdAsync(dto.SaleOrderId) != null)
            throw new InvalidOperationException("Shipment already exists for this SaleOrder.");

        var shipment = _mapper.Map<Shipment>(dto);
        shipment.CreatedDate = DateTime.UtcNow;
        shipment.ShippedDate = null;

        await _repo.AddAsync(shipment);
        await _unitOfWork.SaveChangesAsync();
        return (await GetByIdAsync(shipment.Id))!;
    }

    public async Task<ShipmentDto?> MarkShippedAsync(Guid id)
    {
        var shipment = await _repo.GetByIdAsync(id);
        if (shipment == null)
            return null;

        if (shipment.ShippedDate != null)
            throw new InvalidOperationException("Shipment has already been marked as shipped.");

        var saleOrder = shipment.SaleOrder;
        if (saleOrder.Status != SaleOrderStatus.Packed)
            throw new InvalidOperationException(
                $"Cannot mark shipment as shipped for SaleOrder in '{saleOrder.Status}' status. Must be 'Packed'.");

        var shippedDate = DateTime.UtcNow;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            shipment.ShippedDate = shippedDate;
            saleOrder.Status = SaleOrderStatus.Shipped;
            await _repo.UpdateAsync(shipment);
            await _saleOrderRepo.UpdateAsync(saleOrder);
            await _unitOfWork.SaveChangesAsync();
        });

        await _gateway.NotifyShippedAsync(new ShipmentShippedNotification
        {
            ShipmentId = shipment.Id,
            SaleOrderId = shipment.SaleOrderId,
            SaleOrderNo = saleOrder.OrderNo,
            ShippedDateUtc = shippedDate,
        });

        return _mapper.Map<ShipmentDto>(shipment);
    }
}
