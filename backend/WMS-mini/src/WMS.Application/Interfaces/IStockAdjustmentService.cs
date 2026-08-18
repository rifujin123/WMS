using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IStockAdjustmentService
{
    Task<List<StockAdjustmentDto>> GetAllAsync();
    Task<StockAdjustmentDto?> GetByIdAsync(Guid id);
    Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentDto dto);
    Task<StockAdjustmentDto?> ApproveAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}
