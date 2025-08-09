namespace CodeAgent.Agents;

public class AgentCapabilities
{
    public bool                      SupportsStreaming        { get; set; }
    public bool                      SupportsParallelExecution { get; set; }
    public bool                      RequiresContext           { get; set; }
    public int                       MaxTokens                 { get; set; }
    public List<string>              SupportedLanguages        { get; set; } = new();
    public List<string>              SupportedFrameworks       { get; set; } = new();
    public Dictionary<string, object> CustomCapabilities        { get; set; } = new();
}