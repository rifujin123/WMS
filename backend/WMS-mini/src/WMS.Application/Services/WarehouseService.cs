using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _repo;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public WarehouseService(IWarehouseRepository repo, IMapper mapper, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<WarehouseDto>> GetAllAsync()
    {
        var warehouses = await _repo.GetAllAsync();
        return _mapper.Map<List<WarehouseDto>>(warehouses);
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id)
    {
        var warehouse = await _repo.GetByIdAsync(id);
        if (warehouse == null) return null;
        return _mapper.Map<WarehouseDto>(warehouse);
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto)
    {
        var warehouse = _mapper.Map<Warehouse>(dto);
        await _repo.AddAsync(warehouse);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<WarehouseDto>(warehouse);
    }

    public async Task<WarehouseDto?> UpdateAsync(Guid id, UpdateWarehouseDto dto)
    {
        var warehouse = await _repo.GetByIdAsync(id);
        if (warehouse == null) return null;

        _mapper.Map(dto, warehouse);
        await _repo.UpdateAsync(warehouse);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<WarehouseDto>(warehouse);
    }

    public async Task<DeleteWarehouseResult> DeleteAsync(Guid id)
    {
        var warehouse = await _repo.GetByIdAsync(id);
        if (warehouse == null) return DeleteWarehouseResult.NotFound;

        if (await _repo.HasLocationsAsync(id))
            return DeleteWarehouseResult.HasLocations;

        await _repo.DeleteAsync(warehouse);
        await _unitOfWork.SaveChangesAsync();
        return DeleteWarehouseResult.Success;
    }
}
