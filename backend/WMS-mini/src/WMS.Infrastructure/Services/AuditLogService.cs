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

    public async Task<List<AuditLogDto>> GetAsync(AuditLogQueryDto query)
    {
        var items = await _repository.GetAsync(query);
        return _mapper.Map<List<AuditLogDto>>(items);
    }

    public async Task<List<StatusHistoryDto>> GetStatusHistoryAsync(string entityType, Guid entityId)
    {
        var items = await _repository.GetStatusHistoryAsync(entityType, entityId);
        return _mapper.Map<List<StatusHistoryDto>>(items);
    }

    public async Task<List<StatusHistoryDto>> GetStatusHistoriesAsync(StatusHistoryQueryDto query)
    {
        var items = await _repository.GetStatusHistoriesAsync(query);
        return _mapper.Map<List<StatusHistoryDto>>(items);
    }
}
