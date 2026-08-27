using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IVendorService
{
    Task<List<VendorDto>> GetAllAsync();
    Task<PagedResult<VendorDto>> GetPagedAsync(VendorListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<VendorDto?> GetByIdAsync(Guid id);
    Task<VendorDto> CreateAsync(CreateVendorDto dto);
    Task<VendorDto?> UpdateAsync(Guid id, UpdateVendorDto dto);
    Task<bool> DeleteAsync(Guid id);
}