namespace WMS.Application.Configuration;

public class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    // "Mock" (chạy không cần API key) hoặc "Real" (gọi OpenAI-compatible thật)
    public string Provider { get; init; } = "Mock";

    // Ví dụ: "https://api.openai.com/v1" hoặc "http://localhost:11434/v1" (Ollama)
    public string BaseUrl { get; init; } = string.Empty;

    // Không bắt buộc — Ollama/local chạy không cần key
    public string? ApiKey { get; init; }

    // Ví dụ: "gpt-4o-mini", "llama3.2-vision"
    public string Model { get; init; } = string.Empty;
}
