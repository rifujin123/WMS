using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IBackorderLinkService
{
    Task<List<BackorderLinkDto>> GetAllAsync(Guid? saleOrderId = null, Guid? purchaseOrderId = null, Guid? productId = null);
    Task<BackorderLinkDto?> GetByIdAsync(Guid id);
    Task<BackorderLinkDto> CreateAsync(CreateBackorderLinkDto dto);
    Task<bool> DeleteAsync(Guid id);
}
