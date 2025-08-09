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

The setup service handles automatic agent configuration based on available providers:

| Condition | Action |
|-----------|--------|
| No providers | Error - requires at least one provider |
| Single provider | All agents use that provider |
| Multiple providers, no config | Optimize by capability |
| User configuration provided | Apply user preferences |

## Automatic Parameter Tuning

Each agent type receives optimized parameters for its specific task, even when using the same underlying model.

## Agent Types
| Agent | Responsibility | Temperature | Max Tokens | Capability Required |
|-------|---------------|------------|------------|-------------------|
| Planning | Architecture, design | 0.8 | 4096 | High reasoning |
| Coding | Implementation | 0.5 | 8192 | Code generation |
| Review | Quality checks | 0.3 | 4096 | Analysis |
| Testing | Test generation | 0.2 | 4096 | Fast iteration |
| Documentation | Docs creation | 0.4 | 4096 | Text generation |

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