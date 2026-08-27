using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync();
    Task<PagedResult<CustomerDto>> GetPagedAsync(CustomerListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdAsync(Guid id);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
    Task AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(Customer customer);
}