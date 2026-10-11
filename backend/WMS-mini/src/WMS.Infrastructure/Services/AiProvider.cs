using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class AiProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiProviderOptions _options;
    private readonly WmsDbContext _db;

    public AiProvider(IHttpClientFactory httpClientFactory, IOptions<AiProviderOptions> options, WmsDbContext db)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _db = db;
    }

    public async Task<InvoiceExtractionResult> ExtractInvoiceAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _db.CurrentTenantId;
        string? apiKey = null;
        string? tenantModel = null;

        if (tenantId.HasValue)
        {
            var tenant = await _db.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.Id == tenantId.Value)
                .Select(t => new { t.AiApiKey, t.AiModel })
                .FirstOrDefaultAsync(cancellationToken);

            apiKey = tenant?.AiApiKey;
            tenantModel = tenant?.AiModel;
        }

        // Fallback sang config hệ thống nếu tenant chưa cấu hình riêng
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = _options.ApiKey;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Doanh nghiệp của bạn chưa cấu hình Google Gemini API Key. Vui lòng liên hệ Quản trị viên vào mục Cài đặt để thiết lập khóa API trước khi quét hóa đơn.");
        }

        var modelToUse = !string.IsNullOrWhiteSpace(tenantModel)
            ? tenantModel
            : (string.IsNullOrWhiteSpace(_options.Model) ? "gemini-1.5-flash" : _options.Model);

        // Copy stream vào byte array
        using var memory = new MemoryStream();
        await imageStream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();

        var client = _httpClientFactory.CreateClient("AiProvider");

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        var mime = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream",
        };
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(fileContent, "file", fileName);

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            form.Add(new StringContent(apiKey), "api_key");
        }

        if (!string.IsNullOrWhiteSpace(modelToUse))
        {
            form.Add(new StringContent(modelToUse), "model");
        }

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("/api/v1/invoices/extract", form, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            var serviceUrl = _options.ServiceUrl;
            throw new HttpRequestException($"Không thể kết nối đến Dịch vụ AI (Python Service). Vui lòng đảm bảo dịch vụ đang chạy tại {serviceUrl}.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Dịch vụ AI (Python Service) trả về mã lỗi [{(int)response.StatusCode}]: {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var pyResult = JsonSerializer.Deserialize<PythonInvoiceExtractResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Không thể phân tích dữ liệu trả về từ Dịch vụ AI.");

        return new InvoiceExtractionResult
        {
            InvoiceNumber = pyResult.InvoiceNumber,
            VendorName = pyResult.VendorName,
            InvoiceDate = pyResult.InvoiceDate,
            ConfidenceScore = pyResult.ConfidenceScore,
            LineItems = pyResult.LineItems.Select(item => new InvoiceLineItemDto
            {
                Sku = item.Sku,
                Name = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalAmount = item.TotalAmount,
            }).ToList(),
        };
    }

    private class PythonInvoiceLineItem
    {
        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unit_price")]
        public decimal? UnitPrice { get; set; }

        [JsonPropertyName("total_amount")]
        public decimal? TotalAmount { get; set; }
    }

    private class PythonInvoiceExtractResponse
    {
        [JsonPropertyName("invoice_number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [JsonPropertyName("vendor_name")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("invoice_date")]
        public DateTime? InvoiceDate { get; set; }

        [JsonPropertyName("line_items")]
        public List<PythonInvoiceLineItem> LineItems { get; set; } = new();

        [JsonPropertyName("confidence_score")]
        public double ConfidenceScore { get; set; } = 1.0;
    }
}
