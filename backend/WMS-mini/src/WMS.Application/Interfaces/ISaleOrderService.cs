using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface ISaleOrderService
{
    Task<List<SaleOrderDto>> GetAllAsync();
    Task<SaleOrderDto?> GetByIdAsync(Guid id);
    Task<SaleOrderDto> CreateAsync(CreateSaleOrderDto dto, Guid userId);
    Task<SaleOrderDto?> UpdateAsync(Guid id, CreateSaleOrderDto dto);
    Task<bool> DeleteAsync(Guid id);
}
