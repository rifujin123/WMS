using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(Guid id);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, Guid userId);
    Task<CategoryDto?> UpdateAsync(Guid id,UpdateCategoryDto dto);
    Task<bool> DeleteAsync(Guid id);
}
