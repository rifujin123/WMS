using System.Text.Json;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

public class MockAiProvider : IAiProvider
{
    // Mock trả về dữ liệu cố định đọc từ file JSON mẫu — không cần API key
    public Task<InvoiceExtractionResult> ExtractInvoiceAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "InvoiceMockData", "invoice.json");
        var json = File.ReadAllText(jsonPath);
        var result = JsonSerializer.Deserialize<InvoiceExtractionResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Không đọc được file dữ liệu mẫu invoice.json.");
        return Task.FromResult(result);
    }
}
