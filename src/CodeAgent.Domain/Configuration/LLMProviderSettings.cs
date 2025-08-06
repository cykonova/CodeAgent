namespace CodeAgent.Domain.Configuration;

public class LLMProviderSettings
{
    public string Provider { get; set; } = "OpenAI";
    public string? ApiKey { get; set; }
    public string? ApiUrl { get; set; }
    public string? Model { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int? MaxTokens { get; set; }
}