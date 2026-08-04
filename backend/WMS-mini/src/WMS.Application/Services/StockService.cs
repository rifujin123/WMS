using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.Application.Services;

public class StockService : IStockService
{
    private readonly IStockRepository _repo;
    private readonly IMapper _mapper;

    public StockService(IStockRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<StockDto>> GetAllAsync()
    {
        var results = await _repo.GetAllAsync();
        return _mapper.Map<List<StockDto>>(results);
    }

    public async Task<StockDto?> GetByIdAsync(Guid id)
    {
        var result = await _repo.GetByIdAsync(id);
        if (result == null) return null;
        return _mapper.Map<StockDto>(result);
    }

    public async Task<List<StockDto>> GetByProductAsync(Guid productId)
    {
        var results = await _repo.GetByProductAsync(productId);
        return _mapper.Map<List<StockDto>>(results);
    }

    public async Task<List<StockDto>> GetByLocationAsync(Guid locationId)
    {
        var results = await _repo.GetByLocationAsync(locationId);
        return _mapper.Map<List<StockDto>>(results);
    }
}