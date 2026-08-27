using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync();
    Task<PagedResult<CustomerDto>> GetPagedAsync(CustomerListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(Guid id);
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
    Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerDto dto);
    Task<bool> DeleteAsync(Guid id);
}