using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

public class RealAiProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiProviderOptions _options;

    public RealAiProvider(IHttpClientFactory httpClientFactory, IOptions<AiProviderOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<InvoiceExtractionResult> ExtractInvoiceAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("AiProvider:BaseUrl chưa cấu hình. Set env AiProvider__BaseUrl.");

        // 1. Đọc ảnh thành base64 + đoán MIME type từ đuôi file
        using var memory = new MemoryStream();
        await imageStream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var mime = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "image/jpeg",
        };
        var base64 = Convert.ToBase64String(bytes);

        // 2. Dựng body request theo giao thức OpenAI chat/completions
        var body = new JsonObject
        {
            ["model"] = _options.Model,
            ["response_format"] = new JsonObject { ["type"] = "json_object" },
            ["max_tokens"] = 2000,
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = SystemPrompt
                },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray
                    {
                        new JsonObject { ["type"] = "text", ["text"] = "Hãy trích xuất thông tin từ hóa đơn này." },
                        new JsonObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JsonObject { ["url"] = $"data:{mime};base64,{base64}" }
                        }
                    }
                }
            }
        };

        // 3. Gửi POST tới {BaseUrl}/chat/completions
        var client = _httpClientFactory.CreateClient("AiProvider");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // 4. Đọc chuỗi JSON: choices[0].message.content (JSON dạng chuỗi bên trong)
        var root = JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken))
            ?? throw new InvalidOperationException("AI provider trả về response rỗng.");
        var content = root["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
            ?? throw new InvalidOperationException("AI provider trả về định dạng không mong đợi.");

        // 5. Parse content (chuỗi JSON) thành DTO
        var result = JsonSerializer.Deserialize<InvoiceExtractionResult>(content)
            ?? throw new InvalidOperationException("AI không trích xuất được dữ liệu hóa đơn.");
        return result;
    }

    private const string SystemPrompt =
        "Bạn là trợ lý trích xuất hóa đơn. Trả về CHỈ JSON hợp lệ, không kèm giải thích, " +
        "đúng schema: {\"invoiceNumber\":\"\",\"vendorName\":\"\",\"invoiceDate\":\"yyyy-MM-ddTHH:mm:ss\" " +
        "hoặc null,\"lineItems\":[{\"sku\":\"\",\"name\":\"\",\"quantity\":0}]}. " +
        "sku là mã sản phẩm nếu có trên hóa đơn, ngược lại để rỗng.";
}
