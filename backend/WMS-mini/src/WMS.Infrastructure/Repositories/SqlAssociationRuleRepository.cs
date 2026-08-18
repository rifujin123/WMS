using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlAssociationRuleRepository : IAssociationRuleRepository
{
    private readonly WmsDbContext _db;

    public SqlAssociationRuleRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<AssociationRule>> GetAllAsync()
    {
        return await _db.AssociationRules.ToListAsync();
    }

    public async Task<AssociationRule?> GetByIdAsync(Guid id)
    {
        return await _db.AssociationRules.FindAsync(id);
    }

    public async Task AddAsync(AssociationRule rule)
    {
        await _db.AssociationRules.AddAsync(rule);
    }

    public async Task UpdateAsync(AssociationRule rule)
    {
        _db.AssociationRules.Update(rule);
    }

    public Task DeleteAsync(AssociationRule rule)
    {
        _db.AssociationRules.Remove(rule);
        return Task.CompletedTask;
    }
}
