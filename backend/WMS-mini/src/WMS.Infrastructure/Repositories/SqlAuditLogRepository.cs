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

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(AuditLogQueryDto query, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<AuditLog> auditLogs = _db.AuditLogs.AsNoTracking();

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

        var totalCount = await auditLogs.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await auditLogs
            .OrderByDescending(a => a.OccurredAtUtc)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                ActorUserId = a.ActorUserId,
                ActorDisplayName = a.ActorUser!.FullName,
                ActorAvatarUrl = a.ActorUser != null ? a.ActorUser.AvatarUrl : null,
                OccurredAtUtc = a.OccurredAtUtc,
                OldValuesJson = a.OldValuesJson,
                NewValuesJson = a.NewValuesJson,
                ChangedFieldsJson = a.ChangedFieldsJson,
                CorrelationId = a.CorrelationId,
                RequestPath = a.RequestPath,
            })
            .ToListAsync(cancellationToken);

        return PagedResult<AuditLogDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<List<StatusHistoryDto>> GetStatusHistoryAsync(string entityType, Guid entityId)
    {
        return await _db.StatusHistories
            .AsNoTracking()
            .Where(s => s.EntityType == entityType && s.EntityId == entityId)
            .OrderByDescending(s => s.OccurredAtUtc)
            .Select(s => new StatusHistoryDto
            {
                Id = s.Id,
                EntityType = s.EntityType,
                EntityId = s.EntityId,
                FromStatus = s.FromStatus,
                ToStatus = s.ToStatus,
                Action = s.Action,
                ActorUserId = s.ActorUserId,
                ActorDisplayName = s.ActorUser!.FullName,
                ActorAvatarUrl = s.ActorUser != null ? s.ActorUser.AvatarUrl : null,
                OccurredAtUtc = s.OccurredAtUtc,
                Notes = s.Notes,
                MetadataJson = s.MetadataJson,
            })
            .ToListAsync();
    }

    public async Task<PagedResult<StatusHistoryDto>> GetStatusHistoriesPagedAsync(StatusHistoryQueryDto query, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<StatusHistory> histories = _db.StatusHistories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            histories = histories.Where(s => s.EntityType == query.EntityType);
        if (query.FromUtc.HasValue)
            histories = histories.Where(s => s.OccurredAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue)
            histories = histories.Where(s => s.OccurredAtUtc <= query.ToUtc.Value);

        var totalCount = await histories.CountAsync(cancellationToken);
        var page = query.Page;
        var items = await histories
            .OrderByDescending(s => s.OccurredAtUtc)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StatusHistoryDto
            {
                Id = s.Id,
                EntityType = s.EntityType,
                EntityId = s.EntityId,
                FromStatus = s.FromStatus,
                ToStatus = s.ToStatus,
                Action = s.Action,
                ActorUserId = s.ActorUserId,
                ActorDisplayName = s.ActorUser!.FullName,
                ActorAvatarUrl = s.ActorUser != null ? s.ActorUser.AvatarUrl : null,
                OccurredAtUtc = s.OccurredAtUtc,
                Notes = s.Notes,
                MetadataJson = s.MetadataJson,
            })
            .ToListAsync(cancellationToken);

        return PagedResult<StatusHistoryDto>.Create(items, page, pageSize, totalCount);
    }
}
