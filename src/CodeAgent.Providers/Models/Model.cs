namespace CodeAgent.Providers.Models;

public class Model
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int? MaxTokens { get; set; }
    public int? ContextWindow { get; set; }
    public bool SupportsTools { get; set; }
    public bool SupportsStreaming { get; set; }
    public bool SupportsVision { get; set; }
    public decimal? InputCostPer1kTokens { get; set; }
    public decimal? OutputCostPer1kTokens { get; set; }
    public Dictionary<string, object> Capabilities { get; set; } = new();
}