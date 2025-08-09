using CodeAgent.Providers.Contracts;

namespace CodeAgent.Agents;

public class AgentConfiguration
{
    public string                    AgentId        { get; set; } = string.Empty;
    public string                    Name           { get; set; } = string.Empty;
    public AgentType                 Type           { get; set; }
    public string                    ProviderId     { get; set; } = string.Empty;
    public string                    Model          { get; set; } = string.Empty;
    public double                    Temperature    { get; set; } = 0.5;
    public int                       MaxTokens      { get; set; } = 4096;
    public ILLMProvider?             Provider       { get; set; }
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}