namespace CodeAgent.Agents;

public class OrchestratorResponse
{
    public string              ResponseId          { get; set; } = Guid.NewGuid().ToString();
    public bool                Success             { get; set; }
    public string              Content             { get; set; } = string.Empty;
    public List<AgentResponse> AgentResponses      { get; set; } = new();
    public AgentContext?       Context             { get; set; }
    public string?             ErrorMessage        { get; set; }
    public TimeSpan            TotalExecutionTime  { get; set; }
}