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

## Agent Types
| Agent | Responsibility | Temperature | Max Tokens |
|-------|---------------|------------|------------|
| Planning | Architecture, design | 0.7 | 4096 |
| Coding | Implementation | 0.3 | 8192 |
| Review | Quality checks | 0.2 | 4096 |
| Testing | Test generation | 0.1 | 4096 |
| Documentation | Docs creation | 0.5 | 4096 |

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