using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IAssociationRuleRepository
{
    Task<List<AssociationRule>> GetAllAsync();
    Task<AssociationRule?> GetByIdAsync(Guid id);
    Task AddAsync(AssociationRule rule);
    Task UpdateAsync(AssociationRule rule);
    Task DeleteAsync(AssociationRule rule);
}
