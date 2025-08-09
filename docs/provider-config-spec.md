# Code Agent - Provider & Workflow Configuration Specification

## Overview

The Code Agent system supports multiple LLM providers with fine-grained control over which provider/model handles each agent type. Projects inherit a default configuration but maintain independent settings that can be customized without affecting other projects.

## Supported Providers

### API-Based Providers

| Provider | Models | Use Cases | Strengths |
|----------|--------|-----------|-----------|
| **Anthropic** | Claude 3 Opus, Sonnet, Haiku | Complex reasoning, code review | Large context, nuanced understanding |
| **OpenAI** | GPT-4, GPT-4 Turbo, GPT-3.5 | Code generation, general tasks | Broad capabilities, tool use |
| **Google Gemini** | Gemini Ultra, Pro, Nano | Testing, documentation | Multimodal, fast responses |
| **xAI Grok** | Grok-1, Grok-2 | Real-time coding, humor | Current information, personality |
| **Mistral** | Large, Medium, Small, Mixtral | Documentation, refactoring | Cost-effective, European |
| **Cohere** | Command, Command-R | Search, summarization | Retrieval-augmented generation |
| **Azure OpenAI** | Custom deployments | Enterprise | SLA, compliance |

### Local Providers

| Provider | Models | Use Cases | Strengths |
|----------|--------|-----------|-----------|
| **Ollama** | Any GGUF model | Privacy-sensitive work | No API costs, offline |
| **LM Studio** | Any compatible model | Experimentation | GUI, easy model management |
| **LocalAI** | OpenAI-compatible | Drop-in replacement | API compatibility |
| **Text Gen WebUI** | Various formats | Research | Extensive customization |

## Provider Configuration

### Provider Definition Format
```yaml
# providers.yaml
providers:
  anthropic:
    type: api
    endpoint: https://api.anthropic.com/v1
    auth:
      type: api_key
      key: ${ANTHROPIC_API_KEY}
    models:
      - id: claude-3-opus-20240229
        name: Claude 3 Opus
        contextWindow: 200000
        pricing:
          input: 0.015
          output: 0.075
      - id: claude-3-sonnet-20240229
        name: Claude 3 Sonnet
        contextWindow: 200000
        pricing:
          input: 0.003
          output: 0.015
    capabilities:
      - code_generation
      - code_review
      - testing
      - documentation
      - refactoring
    
  openai:
    type: api
    endpoint: https://api.openai.com/v1
    auth:
      type: api_key
      key: ${OPENAI_API_KEY}
    models:
      - id: gpt-4-turbo-preview
        name: GPT-4 Turbo
        contextWindow: 128000
        pricing:
          input: 0.01
          output: 0.03
      - id: gpt-4
        name: GPT-4
        contextWindow: 8192
        pricing:
          input: 0.03
          output: 0.06
    capabilities:
      - code_generation
      - function_calling
      - vision
      
  gemini:
    type: api
    endpoint: https://generativelanguage.googleapis.com/v1
    auth:
      type: api_key
      key: ${GEMINI_API_KEY}
    models:
      - id: gemini-pro
        name: Gemini Pro
        contextWindow: 32768
        pricing:
          input: 0.0005
          output: 0.0015
      - id: gemini-ultra
        name: Gemini Ultra
        contextWindow: 1000000
        pricing:
          input: 0.007
          output: 0.021
    capabilities:
      - code_generation
      - multimodal
      - long_context
      
  grok:
    type: api
    endpoint: https://api.x.ai/v1
    auth:
      type: api_key
      key: ${GROK_API_KEY}
    models:
      - id: grok-2
        name: Grok 2
        contextWindow: 100000
        pricing:
          input: 0.01
          output: 0.02
    capabilities:
      - code_generation
      - real_time_info
      - humor
      
  ollama:
    type: local
    endpoint: http://localhost:11434
    models:
      - id: codellama:34b
        name: Code Llama 34B
        contextWindow: 16384
      - id: mixtral:8x7b
        name: Mixtral 8x7B
        contextWindow: 32768
      - id: deepseek-coder:33b
        name: DeepSeek Coder 33B
        contextWindow: 16384
    capabilities:
      - code_generation
      - offline
      - privacy
```

## Agent Configuration

### Default Agent Assignment
```yaml
# config/agents.yaml
agents:
  planning:
    description: "Analyzes requirements and creates technical designs"
    default_provider: anthropic
    default_model: claude-3-opus-20240229
    settings:
      temperature: 0.7
      max_tokens: 4096
      system_prompt: |
        You are a senior software architect specializing in system design.
        Focus on scalability, maintainability, and best practices.
    fallback_providers:
      - openai/gpt-4
      - gemini/gemini-ultra
      
  coding:
    description: "Generates production-ready code"
    default_provider: openai
    default_model: gpt-4-turbo-preview
    settings:
      temperature: 0.3
      max_tokens: 8192
      system_prompt: |
        You are an expert programmer. Generate clean, efficient, well-documented code.
        Follow SOLID principles and include error handling.
    fallback_providers:
      - anthropic/claude-3-opus
      - ollama/codellama:34b
      
  review:
    description: "Reviews code for quality and security"
    default_provider: anthropic
    default_model: claude-3-sonnet-20240229
    settings:
      temperature: 0.2
      max_tokens: 4096
      system_prompt: |
        You are a meticulous code reviewer. Check for bugs, security issues,
        performance problems, and adherence to best practices.
    fallback_providers:
      - openai/gpt-4
      - gemini/gemini-pro
      
  testing:
    description: "Generates comprehensive test suites"
    default_provider: gemini
    default_model: gemini-pro
    settings:
      temperature: 0.1
      max_tokens: 4096
      system_prompt: |
        You are a QA engineer. Generate thorough unit tests, integration tests,
        and edge cases. Aim for high code coverage.
    fallback_providers:
      - openai/gpt-3.5-turbo
      - ollama/mixtral:8x7b
      
  documentation:
    description: "Creates clear, comprehensive documentation"
    default_provider: mistral
    default_model: mistral-large
    settings:
      temperature: 0.5
      max_tokens: 4096
      system_prompt: |
        You are a technical writer. Create clear, comprehensive documentation
        including API docs, user guides, and code comments.
    fallback_providers:
      - cohere/command
      - grok/grok-1
```

## Workflow Configuration

### System Default Workflow
```yaml
# config/default-workflow.yaml
workflow:
  name: "Standard Development Workflow"
  version: "1.0"
  
  stages:
    - name: "Requirements Analysis"
      agent: planning
      provider_override: null  # Use agent default
      actions:
        - analyze_requirements
        - create_architecture
        - define_interfaces
      output: 
        - architecture.md
        - api-spec.yaml
        
    - name: "Implementation"
      agent: coding
      provider_override: null
      actions:
        - generate_code
        - implement_features
      output:
        - src/**/*.cs
        
    - name: "Quality Assurance"
      parallel: true
      stages:
        - name: "Code Review"
          agent: review
          actions:
            - security_review
            - performance_review
            - best_practices_check
          output:
            - review-report.md
            
        - name: "Test Generation"
          agent: testing
          actions:
            - unit_tests
            - integration_tests
          output:
            - tests/**/*.cs
            
    - name: "Documentation"
      agent: documentation
      actions:
        - api_documentation
        - user_guide
        - inline_comments
      output:
        - docs/**/*.md
        
  orchestration:
    max_iterations: 3
    feedback_loop: true
    human_approval_required: false
    cost_limit: 10.00  # USD
    time_limit: 300    # seconds
```

### Project-Specific Workflow
```yaml
# projects/my-project/workflow.yaml
workflow:
  name: "My Project Custom Workflow"
  base: "default"  # Inherit from default
  
  # Override specific agents for this project
  agent_overrides:
    planning:
      provider: gemini
      model: gemini-ultra
      settings:
        temperature: 0.8
        
    coding:
      provider: anthropic
      model: claude-3-opus-20240229
      settings:
        temperature: 0.4
        max_tokens: 16384
        
  # Add project-specific stages
  additional_stages:
    - name: "Security Audit"
      after: "Quality Assurance"
      agent: review
      provider_override: 
        provider: openai
        model: gpt-4
      actions:
        - security_audit
        - vulnerability_scan
        
  # Modify orchestration settings
  orchestration:
    human_approval_required: true
    cost_limit: 25.00
    preferred_providers:
      - ollama  # Prefer local models when available
      - anthropic
      - openai
```

## Project Configuration Management

### Project Creation
```csharp
public class ProjectService
{
    public async Task<Project> CreateProject(string name, ProjectTemplate template = null)
    {
        var project = new Project { Name = name };
        
        // Copy default configuration
        var defaultConfig = await LoadDefaultConfiguration();
        var projectConfig = defaultConfig.DeepClone();
        
        // Apply template if provided
        if (template != null)
        {
            projectConfig.MergeWith(template.Configuration);
        }
        
        // Save project-specific configuration
        await SaveProjectConfiguration(project.Id, projectConfig);
        
        return project;
    }
}
```

### Configuration Inheritance Hierarchy
```
System Defaults (config/default-workflow.yaml)
    ↓
User Preferences (~/.codeagent/preferences.yaml)
    ↓
Project Template (templates/{template}/workflow.yaml)
    ↓
Project Configuration (projects/{project}/workflow.yaml)
    ↓
Session Overrides (runtime parameters)
```

### Dynamic Provider Selection
```csharp
public class ProviderSelector
{
    public async Task<ILLMProvider> SelectProvider(
        Agent agent, 
        Project project, 
        ProviderContext context)
    {
        // 1. Check session override
        if (context.SessionOverride != null)
            return GetProvider(context.SessionOverride);
            
        // 2. Check project configuration
        var projectConfig = project.Configuration.GetAgentConfig(agent.Type);
        if (projectConfig?.Provider != null)
            return GetProvider(projectConfig.Provider);
            
        // 3. Check user preferences
        var userPref = UserPreferences.GetAgentPreference(agent.Type);
        if (userPref?.Provider != null)
            return GetProvider(userPref.Provider);
            
        // 4. Use agent default
        return GetProvider(agent.DefaultProvider);
    }
    
    private async Task<ILLMProvider> GetProviderWithFallback(
        string primaryProvider,
        List<string> fallbackProviders)
    {
        try
        {
            var provider = GetProvider(primaryProvider);
            if (await provider.IsAvailable())
                return provider;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Primary provider {primaryProvider} unavailable: {ex.Message}");
        }
        
        // Try fallback providers
        foreach (var fallback in fallbackProviders)
        {
            try
            {
                var provider = GetProvider(fallback);
                if (await provider.IsAvailable())
                    return provider;
            }
            catch { }
        }
        
        throw new NoAvailableProviderException();
    }
}
```

## CLI Configuration Commands

```bash
# Global configuration
codeagent config set default.provider anthropic
codeagent config set default.model claude-3-opus

# Agent-specific configuration
codeagent config set agent.planning.provider gemini
codeagent config set agent.coding.model gpt-4-turbo

# Project configuration
codeagent project config set provider anthropic
codeagent project config set workflow.stages[0].provider openai

# List configurations
codeagent config list
codeagent config list --project my-project

# Test provider connection
codeagent provider test anthropic
codeagent provider test all

# View provider status
codeagent provider status
```

## Web UI Configuration

### Provider Management Screen
```typescript
interface ProviderConfig {
  id: string;
  name: string;
  type: 'api' | 'local';
  endpoint: string;
  apiKey?: string;
  models: Model[];
  isEnabled: boolean;
  priority: number;
}

interface AgentConfig {
  type: string;
  name: string;
  description: string;
  provider: string;
  model: string;
  settings: {
    temperature: number;
    maxTokens: number;
    systemPrompt: string;
  };
  fallbackProviders: string[];
}

// Component for managing providers
const ProviderManager: React.FC = () => {
  const [providers, setProviders] = useState<ProviderConfig[]>([]);
  const [agents, setAgents] = useState<AgentConfig[]>([]);
  
  return (
    <div className="provider-manager">
      <ProviderList 
        providers={providers}
        onEdit={handleEditProvider}
        onTest={handleTestProvider}
      />
      
      <AgentAssignments
        agents={agents}
        providers={providers}
        onChange={handleAgentUpdate}
      />
      
      <WorkflowBuilder
        agents={agents}
        onSave={handleWorkflowSave}
      />
    </div>
  );
};
```

## Cost Management

### Cost Tracking
```yaml
# config/cost-limits.yaml
cost_management:
  global_monthly_limit: 500.00
  
  provider_limits:
    anthropic: 200.00
    openai: 150.00
    gemini: 100.00
    
  project_limits:
    default: 50.00
    enterprise_project: 200.00
    
  agent_preferences:
    # Use cheaper models for routine tasks
    testing:
      prefer_cheap: true
      max_cost_per_request: 0.10
      
    documentation:
      prefer_cheap: true
      max_cost_per_request: 0.05
      
    # Use best models for critical tasks
    planning:
      prefer_quality: true
      max_cost_per_request: 1.00
      
    review:
      prefer_quality: true
      max_cost_per_request: 0.50
```

### Load Balancing Strategies
```yaml
load_balancing:
  strategies:
    round_robin:
      description: "Distribute evenly across providers"
      
    least_cost:
      description: "Choose cheapest available provider"
      
    least_latency:
      description: "Choose fastest responding provider"
      
    quality_first:
      description: "Always use best model, fallback if unavailable"
      
    hybrid:
      description: "Balance cost, speed, and quality"
      weights:
        cost: 0.3
        latency: 0.3
        quality: 0.4
```

## Provider Pools

### Pool Configuration
```yaml
# config/provider-pools.yaml
pools:
  premium:
    name: "Premium Quality"
    providers:
      - anthropic/claude-3-opus
      - openai/gpt-4
      - gemini/gemini-ultra
    selection: quality_first
    
  balanced:
    name: "Balanced Performance"
    providers:
      - anthropic/claude-3-sonnet
      - openai/gpt-4-turbo
      - gemini/gemini-pro
      - mistral/mistral-large
    selection: hybrid
    
  budget:
    name: "Cost Effective"
    providers:
      - mistral/mistral-small
      - openai/gpt-3.5-turbo
      - ollama/mixtral
      - ollama/codellama
    selection: least_cost
    
  local_only:
    name: "Privacy First"
    providers:
      - ollama/codellama:34b
      - ollama/mixtral:8x7b
      - lmstudio/deepseek-coder
    selection: round_robin
    
  realtime:
    name: "Low Latency"
    providers:
      - grok/grok-2
      - openai/gpt-3.5-turbo
      - gemini/gemini-nano
    selection: least_latency
```

### Automatic Pool Assignment
```csharp
public class PoolSelector
{
    public string SelectPool(ProjectContext context)
    {
        // Based on project requirements
        if (context.RequiresPrivacy)
            return "local_only";
            
        if (context.Budget < 10)
            return "budget";
            
        if (context.QualityPriority == Priority.High)
            return "premium";
            
        if (context.ResponseTimeRequirement < TimeSpan.FromSeconds(2))
            return "realtime";
            
        return "balanced";
    }
}
```

## Migration and Compatibility

### Provider Migration
```bash
# Export configuration
codeagent config export > config-backup.yaml

# Migrate from one provider to another
codeagent migrate --from openai --to anthropic --project my-project

# Test compatibility
codeagent compat check --provider gemini --workflow default
```

### Version Management
```yaml
compatibility:
  providers:
    anthropic:
      min_version: "2024-02-01"
      api_version: "2024-02-15"
      
    openai:
      min_version: "v1"
      models:
        gpt-4: "2023-03-01"
        gpt-4-turbo: "2024-01-01"
```

---
*Specification Version: 1.0*  
*Last Updated: [Current Date]*