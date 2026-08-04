using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

namespace WMS.Application.Services;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _repo;
    private readonly IMapper _mapper;

    public LocationService(ILocationRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<LocationDto>> GetAllAsync()
    {
        var locations = await _repo.GetAllAsync();
        return _mapper.Map<List<LocationDto>>(locations);
    }

    public async Task<LocationDto?> GetByIdAsync(Guid id)
    {
        var location = await _repo.GetByIdAsync(id);
        if (location == null) return null;
        return _mapper.Map<LocationDto>(location);
    }

    public async Task<List<LocationDto>> GetByWarehouseAsync(Guid warehouseId)
    {
        var locations = await _repo.GetByWarehouseIdAsync(warehouseId);
        return _mapper.Map<List<LocationDto>>(locations);
    }

    public async Task<LocationDto> CreateAsync(CreateLocationDto dto, Guid userId)
    {
        var location = _mapper.Map<Location>(dto);
        location.CreatedById = userId;
        await _repo.AddAsync(location);
        return _mapper.Map<LocationDto>(location);
    }

    public async Task<LocationDto?> UpdateAsync(Guid id, UpdateLocationDto dto)
    {
        var location = await _repo.GetByIdAsync(id);
        if (location == null) return null;

        _mapper.Map(dto, location);
        await _repo.UpdateAsync(location);
        return _mapper.Map<LocationDto>(location);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var location = await _repo.GetByIdAsync(id);
        if (location == null) return false;

        if (await _repo.HasStockAsync(id))
            throw new InvalidOperationException("Cannot delete location that still has stock.");

        await _repo.DeleteAsync(location);
        return true;
    }
}
