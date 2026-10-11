namespace WMS.Application.Configuration;

public class AiProviderOptions
{
    public const string SectionName = "AiProvider";
    public string ServiceUrl { get; init; } = "http://localhost:8000";
    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com";
    public string? ApiKey { get; init; }
    public string Model { get; init; } = "gemini-1.5-flash";
    public int TimeoutSeconds { get; init; } = 60;
}
