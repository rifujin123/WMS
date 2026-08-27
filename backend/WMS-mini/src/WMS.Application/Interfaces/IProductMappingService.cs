using WMS.Application.DTOs;
using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IProductMappingService
{
    List<ProductMatchDto> MapLines(
        List<InvoiceLineItemDto> lines,
        List<PurchaseOrderDetail> poDetails,
        List<Product> allProducts);
}
