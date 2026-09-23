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
        // (a) Copy dữ liệu vào byte array để có thể đọc đồng thời từ nhiều stream
        using var memory = new MemoryStream();
        await imageStream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();

        // (b) Chạy song song 2 tác vụ ngoài I/O độc lập: (1) AI OCR Extraction, (2) Cloudinary Upload
        var aiTask = Task.Run(async () =>
        {
            using var aiStream = new MemoryStream(bytes);
            return await _aiProvider.ExtractInvoiceAsync(aiStream, fileName, cancellationToken);
        }, cancellationToken);

        var uploadTask = Task.Run(async () =>
        {
            using var uploadStream = new MemoryStream(bytes);
            return await _imageService.UploadAsync(
                uploadStream,
                fileName,
                $"wms/receivings/invoice-{Guid.NewGuid():N}",
                1200,
                1200);
        }, cancellationToken);

        // (c) Đọc DbContext tuần tự trên thread chính để đảm bảo an toàn luồng
        var po = await _poRepo.GetByIdAsync(purchaseOrderId)
            ?? throw new InvalidOperationException("PurchaseOrder not found.");
        var allProducts = await _productRepo.GetAllAsync();

        await Task.WhenAll(aiTask, uploadTask);

        var extraction = await aiTask;
        var imageUrl = await uploadTask;

        // (c) So khớp sản phẩm thông minh với Purchase Order
        var products = _mappingService.MapLines(
            extraction.LineItems,
            po.PurchaseOrderDetails.ToList(),
            allProducts);

        // (d) Trả kết quả hoàn chỉnh
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
