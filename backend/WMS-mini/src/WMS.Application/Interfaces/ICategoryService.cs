using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<PagedResult<CategoryDto>> GetPagedAsync(CategoryListQuery query, int pageSize, CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetByIdAsync(Guid id);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
    Task<CategoryDto?> UpdateAsync(Guid id,UpdateCategoryDto dto);
    Task<bool> DeleteAsync(Guid id);
}
