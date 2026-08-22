using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

public class StockMovementService : IStockMovementService
{
    private readonly IStockMovementRepository _repo;
    private readonly IMapper _mapper;

    public StockMovementService(IStockMovementRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<PagedResult<StockMovementDto>> GetAsync(StockMovementQueryDto query, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await _repo.GetAsync(query, pageSize, cancellationToken);
        return PagedResult<StockMovementDto>.Create(
            result.Items.Select(_mapper.Map<StockMovementDto>),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }
}
