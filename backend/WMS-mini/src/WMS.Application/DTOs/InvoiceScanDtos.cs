namespace WMS.Application.DTOs;

// 1 dòng hàng AI đọc được từ hóa đơn
public class InvoiceLineItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

// Toàn bộ thông tin AI trích xuất từ ảnh hóa đơn
public class InvoiceExtractionResult
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();
}

// 1 gợi ý sản phẩm khi không khớp chính xác SKU
public class ProductSuggestionDto
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool InPo { get; set; } // true = sản phẩm nằm trong PO (ưu tiên)
}

// Kết quả map 1 dòng hóa đơn vào sản phẩm trong DB
public class ProductMatchDto
{
    // null = chưa khớp tự động, cần người dùng chọn từ Suggestions
    public Guid? ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public List<ProductSuggestionDto> Suggestions { get; set; } = new();
}

// Payload trả về frontend sau khi scan
public class InvoiceScanResultDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public string ImageUrl { get; set; } = string.Empty; // URL Cloudinary đã upload
    public List<ProductMatchDto> Products { get; set; } = new();
}
