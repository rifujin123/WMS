using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IRmaRepository
{
    Task<List<Rma>> GetAllAsync();
    Task<Rma?> GetByIdAsync(Guid id);
    Task AddAsync(Rma rma);
    Task UpdateAsync(Rma rma);
    Task DeleteAsync(Rma rma);
}
