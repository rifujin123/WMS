using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly IMapper _mapper;

    public AuditLogService(IAuditLogRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResult<AuditLogDto>> GetAsync(AuditLogQueryDto query, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetAsync(query, pageSize, cancellationToken);
        return PagedResult<AuditLogDto>.Create(
            result.Items.Select(_mapper.Map<AuditLogDto>),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<List<StatusHistoryDto>> GetStatusHistoryAsync(string entityType, Guid entityId)
    {
        var items = await _repository.GetStatusHistoryAsync(entityType, entityId);
        return _mapper.Map<List<StatusHistoryDto>>(items);
    }

    public async Task<PagedResult<StatusHistoryDto>> GetStatusHistoriesAsync(StatusHistoryQueryDto query, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetStatusHistoriesAsync(query, pageSize, cancellationToken);
        return PagedResult<StatusHistoryDto>.Create(
            result.Items.Select(_mapper.Map<StatusHistoryDto>),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }
}
