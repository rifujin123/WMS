using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<PagedResult<AuditLog>> GetAsync(AuditLogQueryDto query, int pageSize, CancellationToken cancellationToken = default);
    Task<List<StatusHistory>> GetStatusHistoryAsync(string entityType, Guid entityId);
    Task<PagedResult<StatusHistory>> GetStatusHistoriesAsync(StatusHistoryQueryDto query, int pageSize, CancellationToken cancellationToken = default);
}
