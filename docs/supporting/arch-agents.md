# Agent Architecture

## Agent Interface
```csharp
public interface IAgent
{
    string Type { get; }
    string Description { get; }
    ILLMProvider Provider { get; set; }
    
    Task<AgentResponse> ProcessAsync(AgentRequest request);
    Task<bool> ValidateRequest(AgentRequest request);
}
```

## Agent Setup Service
```csharp
public class AgentSetupService
{
    private readonly IProviderRegistry _providerRegistry;
    
    public async Task<AgentConfiguration> SetupAgents(WorkflowConfig userConfig = null)
    {
        var providers = await _providerRegistry.GetAvailableProviders();
        
        if (!providers.Any())
            throw new NoProvidersException("No LLM providers configured");
        
        // Zero-configuration mode - most common path
        if (userConfig?.AgentAssignments == null)
        {
            return CreateAutomaticConfig(providers.First());
        }
        
        // User has specified custom agent configuration
        return ApplyUserConfig(userConfig, providers);
    }
    
    private AgentConfiguration CreateAutomaticConfig(ILLMProvider provider)
    {
        // All agents use same provider with task-optimized parameters
        return new AgentConfiguration
        {
            Planning = new AgentSetup(provider, temperature: 0.7),
            Coding = new AgentSetup(provider, temperature: 0.3),
            Review = new AgentSetup(provider, temperature: 0.2),
            Testing = new AgentSetup(provider, temperature: 0.1),
            Documentation = new AgentSetup(provider, temperature: 0.5)
        };
    }
}
```

## Agent Types
| Agent | Responsibility | Temperature | Max Tokens | Capability Required |
|-------|---------------|------------|------------|-------------------|
| Planning | Architecture, design | 0.7 | 4096 | High reasoning |
| Coding | Implementation | 0.3 | 8192 | Code generation |
| Review | Quality checks | 0.2 | 4096 | Analysis |
| Testing | Test generation | 0.1 | 4096 | Fast iteration |
| Documentation | Docs creation | 0.5 | 4096 | Text generation |

## Context Management
```csharp
public class AgentContext
{
    public string ProjectId { get; set; }
    public Dictionary<string, object> Variables { get; set; }
    public List<Message> History { get; set; }
    public int TokensUsed { get; set; }
    public int TokensRemaining { get; set; }
}
```

## Orchestration Flow
1. Request received
2. Agent selection
3. Context preparation
4. Provider assignment
5. Execution
6. Result processing
7. Response aggregation