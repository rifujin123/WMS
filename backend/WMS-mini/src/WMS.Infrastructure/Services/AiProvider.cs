using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using WMS.Application.Configuration;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

public class AiProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiProviderOptions _options;

    public AiProvider(IHttpClientFactory httpClientFactory, IOptions<AiProviderOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<InvoiceExtractionResult> ExtractInvoiceAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("API Key của AI Provider chưa được cấu hình. Vui lòng kiểm tra lại cấu hình hệ thống.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://generativelanguage.googleapis.com"
            : _options.BaseUrl;

        // 1. Đọc ảnh thành base64 + xác định MIME type từ phần mở rộng file
        using var memory = new MemoryStream();
        await imageStream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var mime = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "image/jpeg",
        };
        var base64 = Convert.ToBase64String(bytes);

        // 2. Dựng body request theo chuẩn Google Gemini REST API (generateContent)
        var body = new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["text"] = "Hãy trích xuất thông tin chi tiết từ hóa đơn này."
                        },
                        new JsonObject
                        {
                            ["inlineData"] = new JsonObject
                            {
                                ["mimeType"] = mime,
                                ["data"] = base64
                            }
                        }
                    }
                }
            },
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["text"] = SystemPrompt
                    }
                }
            },
            ["generationConfig"] = new JsonObject
            {
                ["responseMimeType"] = "application/json",
                ["temperature"] = 0.1
            }
        };

        // 3. Gửi POST tới endpoint /v1beta/models/{model}:generateContent
        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-1.5-flash" : _options.Model;
        var client = _httpClientFactory.CreateClient("AiProvider");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v1beta/models/{model}:generateContent")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };

        if (!request.Headers.Contains("x-goog-api-key"))
        {
            request.Headers.Add("x-goog-api-key", _options.ApiKey);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Gemini API trả về lỗi ({(int)response.StatusCode} {response.ReasonPhrase}): {errorBody}");
        }

        // 4. Đọc chuỗi JSON từ candidates[0].content.parts[0].text
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var root = JsonNode.Parse(responseJson)
            ?? throw new InvalidOperationException("Gemini provider trả về response rỗng.");

        var content = root["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Gemini provider không trả về nội dung trích xuất.");

        // 5. Chuẩn hóa chuỗi JSON (loại bỏ markdown fence nếu có)
        var cleanJson = content.Trim();
        if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleanJson = cleanJson[7..];
        }
        else if (cleanJson.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleanJson = cleanJson[3..];
        }
        if (cleanJson.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleanJson = cleanJson[..^3];
        }
        cleanJson = cleanJson.Trim();

        // 6. Parse JSON thành DTO InvoiceExtractionResult
        var result = JsonSerializer.Deserialize<InvoiceExtractionResult>(cleanJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Không thể parse dữ liệu hóa đơn từ Gemini.");

        return result;
    }

    private const string SystemPrompt =
        "Bạn là trợ lý trích xuất hóa đơn. Trả về CHỈ JSON hợp lệ, không kèm giải thích, " +
        "đúng schema: {\"invoiceNumber\":\"\",\"vendorName\":\"\",\"invoiceDate\":\"yyyy-MM-ddTHH:mm:ss\" " +
        "hoặc null,\"lineItems\":[{\"sku\":\"\",\"name\":\"\",\"quantity\":0}]}. " +
        "sku là mã sản phẩm nếu có trên hóa đơn, ngược lại để rỗng.";
}
