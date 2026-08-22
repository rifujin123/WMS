using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlCustomerRepository : ICustomerRepository
{
    private readonly WmsDbContext _db;

    public SqlCustomerRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _db.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<PagedResult<CustomerDto>> GetPagedAsync(
        CustomerListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var customers = _db.Customers.AsNoTracking().AsQueryable();
        var search = query.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            customers = customers.Where(c =>
                c.Name.Contains(search) ||
                (c.Phone != null && c.Phone.Contains(search)) ||
                (c.Email != null && c.Email.Contains(search)));
        }

        var totalCount = await customers.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await customers
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                ContactName = c.ContactName,
                Phone = c.Phone,
                Email = c.Email,
                Address = c.Address,
            })
            .ToListAsync(cancellationToken);

        return PagedResult<CustomerDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _db.Customers.FindAsync(id);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        var query = _db.Customers.Where(c => c.Name == name);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task AddAsync(Customer customer)
    {
        await _db.Customers.AddAsync(customer);
    }

    public async Task UpdateAsync(Customer customer)
    {
        _db.Customers.Update(customer);
    }

    public async Task DeleteAsync(Customer customer)
    {
        _db.Customers.Remove(customer);
    }
}