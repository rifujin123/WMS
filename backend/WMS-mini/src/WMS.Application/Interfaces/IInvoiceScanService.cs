using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IInvoiceScanService
{
    Task<InvoiceScanResultDto> ScanAsync(
        Guid purchaseOrderId,
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
