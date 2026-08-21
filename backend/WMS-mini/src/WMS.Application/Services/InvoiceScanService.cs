using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.Application.Services;

public class InvoiceScanService : IInvoiceScanService
{
    private readonly IAiProvider _aiProvider;
    private readonly IImageService _imageService;
    private readonly IPurchaseOrderRepository _poRepo;
    private readonly IProductRepository _productRepo;
    private readonly IProductMappingService _mappingService;

    public InvoiceScanService(
        IAiProvider aiProvider,
        IImageService imageService,
        IPurchaseOrderRepository poRepo,
        IProductRepository productRepo,
        IProductMappingService mappingService)
    {
        _aiProvider = aiProvider;
        _imageService = imageService;
        _poRepo = poRepo;
        _productRepo = productRepo;
        _mappingService = mappingService;
    }

    public async Task<InvoiceScanResultDto> ScanAsync(
        Guid purchaseOrderId,
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        // (a) Copy ảnh vào MemoryStream — đọc được nhiều lần
        using var memory = new MemoryStream();
        await imageStream.CopyToAsync(memory, cancellationToken);

        // (b) AI trích xuất, retry 1 lần khi lỗi/timeout
        memory.Position = 0;
        InvoiceExtractionResult extraction;
        try
        {
            extraction = await _aiProvider.ExtractInvoiceAsync(memory, fileName, cancellationToken);
        }
        catch
        {
            memory.Position = 0;
            extraction = await _aiProvider.ExtractInvoiceAsync(memory, fileName, cancellationToken);
        }

        // (c) Upload ảnh lên Cloudinary
        memory.Position = 0;
        var imageUrl = await _imageService.UploadAsync(
            memory,
            fileName,
            $"wms/receivings/invoice-{Guid.NewGuid():N}",
            1200,
            1200);

        // (d) Kiểm tra PO tồn tại + lấy danh sách sản phẩm
        var po = await _poRepo.GetByIdAsync(purchaseOrderId)
            ?? throw new InvalidOperationException("PurchaseOrder not found.");

        var allProducts = await _productRepo.GetAllAsync();

        // (e) Map sản phẩm
        var products = _mappingService.MapLines(
            extraction.LineItems,
            po.PurchaseOrderDetails.ToList(),
            allProducts);

        // (f) Trả kết quả
        return new InvoiceScanResultDto
        {
            InvoiceNumber = extraction.InvoiceNumber,
            VendorName = extraction.VendorName,
            InvoiceDate = extraction.InvoiceDate,
            ImageUrl = imageUrl,
            Products = products,
        };
    }
}
