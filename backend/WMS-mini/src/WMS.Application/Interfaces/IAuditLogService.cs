using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IAuditLogService
{
    Task<List<AuditLogDto>> GetAsync(AuditLogQueryDto query);
    Task<List<StatusHistoryDto>> GetStatusHistoryAsync(string entityType, Guid entityId);
    Task<List<StatusHistoryDto>> GetStatusHistoriesAsync(StatusHistoryQueryDto query);
}
