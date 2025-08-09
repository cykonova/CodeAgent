using CodeAgent.Agents.Contracts;
using CodeAgent.Agents.Base;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CodeAgent.Agents.Services;

public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ConcurrentDictionary<string, IAgent> _agents              = new();
    private readonly IAgentSetupService                   _setupService;
    private readonly IWorkflowEngine                      _workflowEngine;
    private readonly ILogger<AgentOrchestrator>           _logger;
    private readonly SemaphoreSlim                        _initializationLock = new(1, 1);
    private          bool                                 _isInitialized;

    public AgentOrchestrator(
        IAgentSetupService       setupService,
        IWorkflowEngine          workflowEngine,
        ILogger<AgentOrchestrator> logger)
    {
        _setupService   = setupService;
        _workflowEngine = workflowEngine;
        _logger         = logger;
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
                return true;

            _logger.LogInformation("Initializing Agent Orchestrator");

            var agents = await _setupService.SetupAgentsAsync(cancellationToken);
            
            foreach (var agent in agents)
            {
                _agents.TryAdd(agent.AgentId, agent);
                _logger.LogInformation("Registered agent {AgentId} of type {Type}", agent.AgentId, agent.Type);
            }

            _isInitialized = true;
            _logger.LogInformation("Agent Orchestrator initialized with {Count} agents", _agents.Count);
            
            return true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<OrchestratorResponse> ProcessCommandAsync(OrchestratorRequest request, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(cancellationToken);
        }

        _logger.LogInformation("Processing command: {Command}", request.Command);

        try
        {
            var workflow = await _workflowEngine.DetermineWorkflowAsync(request, cancellationToken);
            
            if (workflow == null)
            {
                return new OrchestratorResponse
                {
                    Success = false,
                    ErrorMessage = "Unable to determine workflow for command"
                };
            }

            var context = new AgentContext
            {
                SessionId = request.SessionId,
                ProjectId = request.ProjectId,
                WorkingDirectory = request.WorkingDirectory
            };

            var results = new List<AgentResponse>();

            foreach (var step in workflow.Steps)
            {
                var agent = GetAgentForType(step.AgentType);
                
                if (agent == null)
                {
                    _logger.LogWarning("No agent available for type {AgentType}", step.AgentType);
                    continue;
                }

                var agentRequest = new AgentRequest
                {
                    Command = step.Command,
                    Content = step.Content ?? request.Content,
                    Context = context,
                    Parameters = step.Parameters
                };

                var agentResponse = await agent.ExecuteAsync(agentRequest, cancellationToken);
                results.Add(agentResponse);

                if (!agentResponse.Success && step.IsRequired)
                {
                    return new OrchestratorResponse
                    {
                        Success = false,
                        ErrorMessage = $"Required step failed: {step.Name}",
                        AgentResponses = results
                    };
                }

                context = agentResponse.UpdatedContext;
                
                context.History.Add(new ContextMessage
                {
                    AgentId = agent.AgentId,
                    Role = "assistant",
                    Content = agentResponse.Content
                });
            }

            return new OrchestratorResponse
            {
                Success = true,
                Content = AggregateResults(results),
                AgentResponses = results,
                Context = context
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command");
            return new OrchestratorResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<OrchestratorResponse> ExecuteWorkflowAsync(string workflowId, WorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowEngine.GetWorkflowAsync(workflowId, cancellationToken);
        
        if (workflow == null)
        {
            return new OrchestratorResponse
            {
                Success = false,
                ErrorMessage = $"Workflow {workflowId} not found"
            };
        }

        var orchestratorRequest = new OrchestratorRequest
        {
            Command = workflow.Name,
            Content = request.Content,
            SessionId = request.SessionId,
            ProjectId = request.ProjectId,
            WorkingDirectory = request.WorkingDirectory
        };

        return await ProcessCommandAsync(orchestratorRequest, cancellationToken);
    }

    public IAgent? GetAgent(string agentId)
    {
        return _agents.TryGetValue(agentId, out var agent) ? agent : null;
    }

    public IAgent? GetAgentForType(AgentType type)
    {
        return _agents.Values.FirstOrDefault(a => a.Type == type);
    }

    public IEnumerable<IAgent> GetAllAgents()
    {
        return _agents.Values;
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down Agent Orchestrator");
        
        var shutdownTasks = _agents.Values.Select(agent => agent.ShutdownAsync(cancellationToken));
        await Task.WhenAll(shutdownTasks);
        
        _agents.Clear();
        _isInitialized = false;
    }

    private string AggregateResults(List<AgentResponse> responses)
    {
        var successfulResponses = responses.Where(r => r.Success).Select(r => r.Content);
        return string.Join("\n\n---\n\n", successfulResponses);
    }
}

