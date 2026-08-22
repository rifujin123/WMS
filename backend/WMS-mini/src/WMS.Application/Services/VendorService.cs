using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

namespace WMS.Application.Services;

public class VendorService : IVendorService
{
    private readonly IVendorRepository _repo;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public VendorService(IVendorRepository repo, IMapper mapper, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<VendorDto>> GetAllAsync()
    {
        var vendors = await _repo.GetAllAsync();
        return _mapper.Map<List<VendorDto>>(vendors);
    }

    public Task<PagedResult<VendorDto>> GetPagedAsync(
        VendorListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _repo.GetPagedAsync(query, pageSize, cancellationToken);

    public async Task<VendorDto?> GetByIdAsync(Guid id)
    {
        var vendor = await _repo.GetByIdAsync(id);
        if (vendor == null) return null;
        return _mapper.Map<VendorDto>(vendor);
    }

    public async Task<VendorDto> CreateAsync(CreateVendorDto dto)
    {
        var name = dto.Name.Trim();
        if (await _repo.ExistsByNameAsync(name))
            throw new InvalidOperationException($"Nhà cung cấp '{name}' đã tồn tại.");

        var vendor = _mapper.Map<Vendor>(dto);
        vendor.Name = name;
        await _repo.AddAsync(vendor);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<VendorDto>(vendor);
    }

    public async Task<VendorDto?> UpdateAsync(Guid id, UpdateVendorDto dto)
    {
        var vendor = await _repo.GetByIdAsync(id);
        if (vendor == null) return null;

        var name = dto.Name.Trim();
        if (await _repo.ExistsByNameAsync(name, excludeId: id))
            throw new InvalidOperationException($"Nhà cung cấp '{name}' đã tồn tại.");

        _mapper.Map(dto, vendor);
        vendor.Name = name;
        await _repo.UpdateAsync(vendor);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<VendorDto>(vendor);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var vendor = await _repo.GetByIdAsync(id);
        if (vendor == null) return false;
        await _repo.DeleteAsync(vendor);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}