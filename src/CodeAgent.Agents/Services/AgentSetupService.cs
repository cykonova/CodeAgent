using CodeAgent.Agents.Contracts;
using CodeAgent.Agents.Implementations;
using CodeAgent.Providers.Contracts;
using CodeAgent.Providers.Services;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Agents.Services;

public class AgentSetupService : IAgentSetupService
{
    private readonly IProviderRegistry         _providerRegistry;
    private readonly IAgentFactory             _agentFactory;
    private readonly ILogger<AgentSetupService> _logger;
    private readonly AgentSetupConfiguration   _configuration;

    public AgentSetupService(
        IProviderRegistry         providerRegistry,
        IAgentFactory             agentFactory,
        ILogger<AgentSetupService> logger,
        AgentSetupConfiguration?  configuration = null)
    {
        _providerRegistry = providerRegistry;
        _agentFactory     = agentFactory;
        _logger           = logger;
        _configuration    = configuration ?? new AgentSetupConfiguration();
    }

    public async Task<List<IAgent>> SetupAgentsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting agent setup");
        
        var availableProviders = _providerRegistry.GetAllProviders();
        
        if (!availableProviders.Any())
        {
            throw new InvalidOperationException("No providers available. Please configure at least one provider.");
        }

        var agents = new List<IAgent>();
        
        if (_configuration.UseCustomAgentAssignment && _configuration.AgentConfigurations.Any())
        {
            agents = await SetupCustomAgentsAsync(_configuration.AgentConfigurations, cancellationToken);
        }
        else
        {
            agents = await SetupAutomaticAgentsAsync(availableProviders, cancellationToken);
        }

        _logger.LogInformation("Agent setup completed with {Count} agents", agents.Count);
        return agents;
    }

    private async Task<List<IAgent>> SetupAutomaticAgentsAsync(
        IEnumerable<ILLMProvider> providers, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Using automatic agent assignment");
        
        var primaryProvider = providers.First();
        var agents = new List<IAgent>();
        
        var agentTypes = Enum.GetValues<AgentType>().Where(t => t != AgentType.Custom);
        
        foreach (var agentType in agentTypes)
        {
            var config = CreateDefaultConfiguration(agentType, primaryProvider);
            var agent = await _agentFactory.CreateAgentAsync(agentType, cancellationToken);
            
            if (agent != null)
            {
                await agent.InitializeAsync(config, cancellationToken);
                agents.Add(agent);
                _logger.LogInformation("Created {Type} agent using provider {Provider}", 
                    agentType, primaryProvider.Name);
            }
        }
        
        return agents;
    }

    private async Task<List<IAgent>> SetupCustomAgentsAsync(
        List<AgentConfiguration> configurations,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Using custom agent assignment");
        
        var agents = new List<IAgent>();
        
        foreach (var config in configurations)
        {
            var provider = _providerRegistry.GetProvider(config.ProviderId);
            
            if (provider == null)
            {
                _logger.LogWarning("Provider {ProviderId} not found for agent {AgentId}", 
                    config.ProviderId, config.AgentId);
                continue;
            }
            
            config.Provider = provider;
            var agent = await _agentFactory.CreateAgentAsync(config.Type, cancellationToken);
            
            if (agent != null)
            {
                await agent.InitializeAsync(config, cancellationToken);
                agents.Add(agent);
                _logger.LogInformation("Created custom {Type} agent {AgentId} using provider {Provider}", 
                    config.Type, config.AgentId, provider.Name);
            }
        }
        
        return agents;
    }

    private AgentConfiguration CreateDefaultConfiguration(AgentType type, ILLMProvider provider)
    {
        var temperatures = new Dictionary<AgentType, double>
        {
            { AgentType.Planning, 0.7 },
            { AgentType.Coding, 0.3 },
            { AgentType.Review, 0.2 },
            { AgentType.Testing, 0.1 },
            { AgentType.Documentation, 0.5 }
        };

        return new AgentConfiguration
        {
            AgentId = $"{type.ToString().ToLower()}-{Guid.NewGuid():N}",
            Name = $"{type} Agent",
            Type = type,
            ProviderId = provider.ProviderId,
            Provider = provider,
            Temperature = temperatures.GetValueOrDefault(type, 0.5),
            MaxTokens = 4096
        };
    }
}