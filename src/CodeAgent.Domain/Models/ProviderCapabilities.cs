namespace CodeAgent.Domain.Models;

public class ProviderCapabilities
{
    public string Name { get; set; } = string.Empty;
    public bool SupportsStreaming { get; set; }
    public bool SupportsFunctionCalling { get; set; }
    public bool SupportsVision { get; set; }
    public bool SupportsEmbeddings { get; set; }
    public int MaxTokens { get; set; }
    public int MaxContextLength { get; set; }
    public List<string> AvailableModels { get; set; } = new();
    public Dictionary<string, object> CustomCapabilities { get; set; } = new();
}