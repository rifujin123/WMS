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

    public async Task<List<StockMovementDto>> GetAsync(StockMovementQueryDto query)
    {
        var movements = await _repo.GetAsync(query);
        return _mapper.Map<List<StockMovementDto>>(movements);
    }
}
