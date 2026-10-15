using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IStockTransferService
{
    Task<List<StockTransferDto>> GetAllAsync();
    Task<StockTransferDto?> GetByIdAsync(Guid id);
    Task<StockTransferDto> CreateAsync(CreateStockTransferDto dto);
    Task<StockTransferDto> ShipAsync(Guid id);
    Task<StockTransferDto> ReceiveAsync(Guid id, ReceiveStockTransferDto dto);
    Task<bool> CancelAsync(Guid id);
}
