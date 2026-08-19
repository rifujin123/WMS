using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlCategoryRepository : ICategoryRepository
{
    private readonly WmsDbContext _db;

    public SqlCategoryRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _db.Categories.ToListAsync();
    }

    public async Task<PagedResult<CategoryDto>> GetPagedAsync(
        CategoryListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var categories = _db.Categories.AsNoTracking().AsQueryable();
        var search = query.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            categories = categories.Where(c => c.Name.Contains(search));
        }

        var totalCount = await categories.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await categories
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
            })
            .ToListAsync(cancellationToken);

        return PagedResult<CategoryDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _db.Categories.FindAsync(id);
    }

    public async Task AddAsync(Category category)
    {
        await _db.Categories.AddAsync(category);
    }

    public async Task UpdateAsync(Category category)
    {
        _db.Categories.Update(category);
    }

    public async Task DeleteAsync(Category category)
    {
        _db.Categories.Remove(category);
    }

    public async Task<bool> HasProductsAsync(Guid categoryId)
    {
        return await _db.Products.AnyAsync(p => p.CategoryId == categoryId);
    }
}
