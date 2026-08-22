using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetAsync(AuditLogQueryDto query, int pageSize, CancellationToken cancellationToken = default);
    Task<List<StatusHistoryDto>> GetStatusHistoryAsync(string entityType, Guid entityId);
    Task<PagedResult<StatusHistoryDto>> GetStatusHistoriesAsync(StatusHistoryQueryDto query, int pageSize, CancellationToken cancellationToken = default);
}
