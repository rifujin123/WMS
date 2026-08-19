using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync();
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateProductDto dto, Stream? imageStream = null, string? imageFileName = null);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto, Stream? imageStream = null, string? imageFileName = null);
    Task<bool> DeleteAsync(Guid id);
    Task<string?> UploadImageAsync(Guid id, Stream fileStream, string fileName);
}
