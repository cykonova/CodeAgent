namespace CodeAgent.Agents.Services;

public class AgentSetupConfiguration
{
    public bool                      UseCustomAgentAssignment { get; set; }
    public List<AgentConfiguration> AgentConfigurations      { get; set; } = new();
}