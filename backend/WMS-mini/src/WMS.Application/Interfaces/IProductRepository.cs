using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id);
    Task<List<Guid>> GetExistingIdsAsync(List<Guid> productIds);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
    Task<bool> HasReferencesAsync(Guid productId);
}
