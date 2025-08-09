namespace CodeAgent.Agents;

public interface IAgentOrchestrator
{
    Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<AgentInfo>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default);
    Task<bool> RegisterAgentAsync(AgentInfo agentInfo, CancellationToken cancellationToken = default);
}

public class AgentResult
{
    public Guid AgentId { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, object>? Output { get; set; }
    public string? Error { get; set; }
    public int TokensUsed { get; set; }
    public int TokensGenerated { get; set; }
    public TimeSpan Duration { get; set; }
}

public class AgentInfo
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object> Capabilities { get; set; } = new();
}