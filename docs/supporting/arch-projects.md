# Project Architecture

## Project Structure

| Configuration Level | Files Created |
|--------------------|---------------|
| Minimal | `data/history.json` only |
| Basic | Adds `config.yaml` |
| Advanced | Adds `workflow.yaml`, `agents.yaml` |
| Full | Adds `sandbox/` directory |

## Configuration Hierarchy
```csharp
public class ConfigurationResolver
{
    public T GetValue<T>(string key)
    {
        // Priority order:
        // 1. Session override
        // 2. Project config
        // 3. User preferences
        // 4. Template config
        // 5. System defaults
        
        return value;
    }
}
```

## Project Model
```csharp
public class Project
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Language { get; set; }
    public string Framework { get; set; }
    public WorkflowConfig Workflow { get; set; }
    public Dictionary<string, AgentConfig> Agents { get; set; }
    public CostLimits Limits { get; set; }
    public SandboxConfig Sandbox { get; set; }
}
```

## Workflow Stages
```yaml
stages:
  - name: Planning
    agent: planning
    provider: anthropic/claude-3-opus
    actions: [analyze, design]
    
  - name: Implementation
    agent: coding
    provider: openai/gpt-4-turbo
    actions: [generate, refactor]
    
  - name: Review
    parallel: true
    stages:
      - agent: review
      - agent: testing
```

## Cost Tracking
- Token counting per request
- Price calculation
- Budget enforcement
- Usage reports