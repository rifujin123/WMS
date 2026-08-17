using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetAsync(AuditLogQueryDto query);
    Task<List<StatusHistory>> GetStatusHistoryAsync(string entityType, Guid entityId);
    Task<List<StatusHistory>> GetStatusHistoriesAsync(StatusHistoryQueryDto query);
}
