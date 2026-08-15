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
    private readonly IMapper _mapper;

    public ShipmentService(
        IShipmentRepository repo,
        ISaleOrderRepository saleOrderRepo,
        IMapper mapper)
    {
        _repo = repo;
        _saleOrderRepo = saleOrderRepo;
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

    public async Task<ShipmentDto> CreateAsync(CreateShipmentDto dto, Guid userId)
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
        shipment.CreatedById = userId;
        shipment.CreatedDate = DateTime.UtcNow;
        shipment.ShippedDate = null;

        await _repo.AddAsync(shipment);
        return (await GetByIdAsync(shipment.Id))!;
    }
}
