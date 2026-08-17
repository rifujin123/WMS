using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class SqlAuditLogRepository : IAuditLogRepository
{
    private readonly WmsDbContext _db;

    public SqlAuditLogRepository(WmsDbContext db)
    {
        _db = db;
    }

    public async Task<List<AuditLog>> GetAsync(AuditLogQueryDto query)
    {
        IQueryable<AuditLog> auditLogs = _db.AuditLogs
            .AsNoTracking()
            .Include(a => a.ActorUser);

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            auditLogs = auditLogs.Where(a => a.EntityType == query.EntityType);
        if (query.EntityId.HasValue)
            auditLogs = auditLogs.Where(a => a.EntityId == query.EntityId.Value);
        if (query.ActorId.HasValue)
            auditLogs = auditLogs.Where(a => a.ActorUserId == query.ActorId.Value);
        if (query.FromUtc.HasValue)
            auditLogs = auditLogs.Where(a => a.OccurredAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue)
            auditLogs = auditLogs.Where(a => a.OccurredAtUtc <= query.ToUtc.Value);

        return await auditLogs
            .OrderByDescending(a => a.OccurredAtUtc)
            .ToListAsync();
    }

    public async Task<List<StatusHistory>> GetStatusHistoryAsync(string entityType, Guid entityId)
    {
        return await _db.StatusHistories
            .AsNoTracking()
            .Include(s => s.ActorUser)
            .Where(s => s.EntityType == entityType && s.EntityId == entityId)
            .OrderByDescending(s => s.OccurredAtUtc)
            .ToListAsync();
    }

    public async Task<List<StatusHistory>> GetStatusHistoriesAsync(StatusHistoryQueryDto query)
    {
        IQueryable<StatusHistory> histories = _db.StatusHistories
            .AsNoTracking()
            .Include(s => s.ActorUser);

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            histories = histories.Where(s => s.EntityType == query.EntityType);
        if (query.FromUtc.HasValue)
            histories = histories.Where(s => s.OccurredAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue)
            histories = histories.Where(s => s.OccurredAtUtc <= query.ToUtc.Value);

        return await histories
            .OrderByDescending(s => s.OccurredAtUtc)
            .ToListAsync();
    }
}
