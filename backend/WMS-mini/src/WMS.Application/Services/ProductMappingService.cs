using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

namespace WMS.Application.Services;

public class ProductMappingService : IProductMappingService
{
    public List<ProductMatchDto> MapLines(
        List<InvoiceLineItemDto> lines,
        List<PurchaseOrderDetail> poDetails,
        List<Product> allProducts)
    {
        // Sản phẩm thuộc PO (dedupe theo Id)
        var poProducts = poDetails
            .Where(d => d.Product != null)
            .Select(d => d.Product!)
            .DistinctBy(p => p.Id)
            .ToList();

        // Tập SKU trong PO (chuẩn hóa: trim + bỏ hoa thường)
        var poSkuSet = poProducts
            .Select(p => p.Sku.Trim().ToLowerInvariant())
            .ToHashSet();

        var result = new List<ProductMatchDto>();

        foreach (var line in lines)
        {
            var sku = line.Sku.Trim().ToLowerInvariant();

            // (1) Khớp chính xác SKU: ưu tiên trong PO, rồi toàn bộ
            var exact = poProducts.FirstOrDefault(p => p.Sku.Trim().ToLowerInvariant() == sku)
                        ?? allProducts.FirstOrDefault(p => p.Sku.Trim().ToLowerInvariant() == sku);

            if (exact != null)
            {
                result.Add(new ProductMatchDto
                {
                    ProductId = exact.Id,
                    Sku = exact.Sku,
                    Name = exact.Name,
                    Quantity = line.Quantity,
                    Suggestions = new List<ProductSuggestionDto>(),
                });
                continue;
            }

            // (2) Không khớp → gợi ý top-3 theo tên, ưu tiên trong PO
            var suggestions = allProducts
                .Select(p => new
                {
                    Product = p,
                    Score = ScoreName(line.Name, p.Name)
                            + (poSkuSet.Contains(p.Sku.Trim().ToLowerInvariant()) ? 10 : 0),
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Product.Name)
                .Take(3)
                .Select(x => new ProductSuggestionDto
                {
                    ProductId = x.Product.Id,
                    Sku = x.Product.Sku,
                    Name = x.Product.Name,
                    InPo = poSkuSet.Contains(x.Product.Sku.Trim().ToLowerInvariant()),
                })
                .ToList();

            result.Add(new ProductMatchDto
            {
                ProductId = null,
                Sku = line.Sku,
                Name = line.Name,
                Quantity = line.Quantity,
                Suggestions = suggestions,
            });
        }

        return result;
    }

    private static int ScoreName(string extractedName, string productName)
    {
        var a = extractedName.Trim().ToLowerInvariant();
        var b = productName.Trim().ToLowerInvariant();
        if (a == b) return 100;
        if (a.Contains(b) || b.Contains(a))
            return Math.Min(a.Length, b.Length);
        return 0;
    }
}
