namespace CodeAgent.Providers.Claude;

public class ClaudeOptions
{
    public string? ApiKey { get; set; }
    public string? DefaultModel { get; set; } = "claude-3-5-sonnet-20241022";
    public string? BaseUrl { get; set; } = "https://api.anthropic.com/v1";
}