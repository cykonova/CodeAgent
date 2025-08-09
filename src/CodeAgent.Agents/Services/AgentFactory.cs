using CodeAgent.Agents.Contracts;
using CodeAgent.Agents.Implementations;
using CodeAgent.Agents.Base;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Agents.Services;

public class AgentFactory : IAgentFactory
{
    private readonly IServiceProvider       _serviceProvider;
    private readonly ILogger<AgentFactory> _logger;

    public AgentFactory(IServiceProvider serviceProvider, ILogger<AgentFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger          = logger;
    }

    public Task<IAgent?> CreateAgentAsync(AgentType type, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating agent of type {AgentType}", type);
        
        IAgent? agent = type switch
        {
            AgentType.Planning => new PlanningAgent(CreateLogger<PlanningAgent>()),
            AgentType.Coding => new CodingAgent(CreateLogger<CodingAgent>()),
            AgentType.Review => new ReviewAgent(CreateLogger<ReviewAgent>()),
            AgentType.Testing => new TestingAgent(CreateLogger<TestingAgent>()),
            AgentType.Documentation => new DocumentationAgent(CreateLogger<DocumentationAgent>()),
            AgentType.Custom => null,
            _ => null
        };
        
        if (agent != null)
        {
            _logger.LogInformation("Created agent of type {AgentType}", type);
        }
        else
        {
            _logger.LogWarning("Unable to create agent of type {AgentType}", type);
        }
        
        return Task.FromResult(agent);
    }

    public Task<IAgent?> CreateCustomAgentAsync(string agentClass, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating custom agent of class {AgentClass}", agentClass);
        
        try
        {
            var agentType = Type.GetType(agentClass);
            
            if (agentType == null || !typeof(IAgent).IsAssignableFrom(agentType))
            {
                _logger.LogError("Invalid agent class {AgentClass}", agentClass);
                return Task.FromResult<IAgent?>(null);
            }
            
            var agent = Activator.CreateInstance(agentType) as IAgent;
            
            if (agent != null)
            {
                _logger.LogInformation("Created custom agent of class {AgentClass}", agentClass);
            }
            
            return Task.FromResult(agent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create custom agent of class {AgentClass}", agentClass);
            return Task.FromResult<IAgent?>(null);
        }
    }

    private ILogger<T> CreateLogger<T>() where T : class
    {
        var loggerFactory = _serviceProvider.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        if (loggerFactory != null)
        {
            return loggerFactory.CreateLogger<T>();
        }
        
        // Fallback to null logger if no logger factory is available
        return new NullLogger<T>();
    }
    
    private class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}