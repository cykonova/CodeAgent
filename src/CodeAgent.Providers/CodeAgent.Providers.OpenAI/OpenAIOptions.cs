namespace CodeAgent.Providers.OpenAI;

public class OpenAIOptions
{
    public string? ApiKey { get; set; }
    public string? DefaultModel { get; set; } = "gpt-3.5-turbo";
    public string? BaseUrl { get; set; } = "https://api.openai.com/v1";
}