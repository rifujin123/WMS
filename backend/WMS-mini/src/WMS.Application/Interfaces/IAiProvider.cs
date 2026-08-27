using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IAiProvider
{
    Task<InvoiceExtractionResult> ExtractInvoiceAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
