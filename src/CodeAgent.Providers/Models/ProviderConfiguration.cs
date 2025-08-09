namespace CodeAgent.Providers.Models;

public class ProviderConfiguration
{
    public string ProviderId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? Organization { get; set; }
    public int? MaxRetries { get; set; } = 3;
    public int? TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}