using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlVendorRepository : IVendorRepository
{
    private readonly WmsDbContext _db;

    public SqlVendorRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Vendor>> GetAllAsync()
    {
        return await _db.Vendors.AsNoTracking().OrderBy(v => v.Name).ToListAsync();
    }

    public async Task<PagedResult<VendorDto>> GetPagedAsync(
        VendorListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var vendors = _db.Vendors.AsNoTracking().AsQueryable();
        var search = query.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            vendors = vendors.Where(v =>
                v.Name.Contains(search) ||
                (v.Phone != null && v.Phone.Contains(search)) ||
                (v.Email != null && v.Email.Contains(search)));
        }

        var totalCount = await vendors.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await vendors
            .OrderBy(v => v.Name)
            .ThenBy(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VendorDto
            {
                Id = v.Id,
                Name = v.Name,
                ContactName = v.ContactName,
                Phone = v.Phone,
                Email = v.Email,
                Address = v.Address,
            })
            .ToListAsync(cancellationToken);

        return PagedResult<VendorDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<Vendor?> GetByIdAsync(Guid id)
    {
        return await _db.Vendors.FindAsync(id);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        var query = _db.Vendors.Where(v => v.Name == name);
        if (excludeId.HasValue)
        {
            query = query.Where(v => v.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task AddAsync(Vendor vendor)
    {
        await _db.Vendors.AddAsync(vendor);
    }

    public async Task UpdateAsync(Vendor vendor)
    {
        _db.Vendors.Update(vendor);
    }

    public async Task DeleteAsync(Vendor vendor)
    {
        _db.Vendors.Remove(vendor);
    }
}