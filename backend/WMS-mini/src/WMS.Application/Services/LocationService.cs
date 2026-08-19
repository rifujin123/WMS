using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

namespace WMS.Application.Services;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _repo;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public LocationService(ILocationRepository repo, IMapper mapper, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<LocationDto>> GetAllAsync()
    {
        var locations = await _repo.GetAllAsync();
        return _mapper.Map<List<LocationDto>>(locations);
    }

    public Task<PagedResult<LocationDto>> GetPagedAsync(
        LocationListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _repo.GetPagedAsync(query, pageSize, cancellationToken);

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

    public async Task<LocationDto> CreateAsync(CreateLocationDto dto)
    {
        var existing = await _repo.GetByWarehouseAndCodeAsync(dto.WarehouseId, dto.Code);
        if (existing != null)
            throw new InvalidOperationException("Location code already exists in this warehouse.");

        var location = _mapper.Map<Location>(dto);
        await _repo.AddAsync(location);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<LocationDto>(location);
    }

    public async Task<LocationDto?> UpdateAsync(Guid id, UpdateLocationDto dto)
    {
        var location = await _repo.GetByIdAsync(id);
        if (location == null) return null;

        if (!string.Equals(location.Code, dto.Code, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _repo.GetByWarehouseAndCodeAsync(location.WarehouseId, dto.Code);
            if (existing != null)
                throw new InvalidOperationException("Location code already exists in this warehouse.");
        }

        _mapper.Map(dto, location);
        await _repo.UpdateAsync(location);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<LocationDto>(location);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var location = await _repo.GetByIdAsync(id);
        if (location == null) return false;

        if (await _repo.HasStockAsync(id))
            throw new InvalidOperationException("Cannot delete location that has stock.");

        await _repo.DeleteAsync(location);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
