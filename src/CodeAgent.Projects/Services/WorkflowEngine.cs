using System.Collections.Concurrent;
using CodeAgent.Agents;
using CodeAgent.Projects.Interfaces;
using CodeAgent.Projects.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Projects.Services;

public class WorkflowEngine : IWorkflowEngine
{
    private readonly ILogger<WorkflowEngine> _logger;
    private readonly IAgentOrchestrator _agentOrchestrator;
    private readonly IProjectService _projectService;
    private readonly ICostTracker _costTracker;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runningWorkflows = new();

    public event EventHandler<WorkflowEventArgs>? WorkflowStateChanged;
    public event EventHandler<StageEventArgs>? StageStateChanged;

    public WorkflowEngine(
        ILogger<WorkflowEngine> logger,
        IAgentOrchestrator agentOrchestrator,
        IProjectService projectService,
        ICostTracker costTracker)
    {
        _logger = logger;
        _agentOrchestrator = agentOrchestrator;
        _projectService = projectService;
        _costTracker = costTracker;
    }

    public async Task<ProjectRun> ExecuteWorkflowAsync(Guid projectId, WorkflowConfiguration workflow, CancellationToken cancellationToken = default)
    {
        var run = new ProjectRun
        {
            StartedAt = DateTime.UtcNow,
            Status = ProjectStatus.Running
        };

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runningWorkflows[run.Id] = cts;

        try
        {
            OnWorkflowStateChanged(projectId, run.Id, ProjectStatus.Running, "Workflow started");

            var executionPlan = await GetExecutionPlanAsync(workflow, new Dictionary<string, object>());
            var context = new Dictionary<string, object>
            {
                ["projectId"] = projectId,
                ["runId"] = run.Id,
                ["workflow"] = workflow
            };

            foreach (var stage in executionPlan)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    run.Status = ProjectStatus.Cancelled;
                    break;
                }

                var stageResult = await ExecuteStageAsync(projectId, stage, context, cts.Token);
                run.StageResults.Add(stageResult);

                if (stageResult.Status == StageStatus.Failed)
                {
                    run.Status = ProjectStatus.Failed;
                    run.ErrorMessage = stageResult.ErrorMessage;
                    break;
                }

                // Update context with stage output
                foreach (var kvp in stageResult.Output)
                {
                    context[$"stage.{stage.Name}.{kvp.Key}"] = kvp.Value;
                }
            }

            if (run.Status == ProjectStatus.Running)
            {
                run.Status = ProjectStatus.Completed;
            }

            run.CompletedAt = DateTime.UtcNow;
            
            // Update project state
            var state = await _projectService.GetProjectStateAsync(projectId, cancellationToken);
            state.Status = run.Status;
            state.LastRunAt = run.StartedAt;
            state.LastRunDuration = run.CompletedAt - run.StartedAt;
            state.RunHistory.Insert(0, run);
            
            // Keep only last 100 runs
            if (state.RunHistory.Count > 100)
            {
                state.RunHistory.RemoveRange(100, state.RunHistory.Count - 100);
            }

            await _projectService.UpdateProjectStateAsync(projectId, state, cancellationToken);

            OnWorkflowStateChanged(projectId, run.Id, run.Status, 
                run.Status == ProjectStatus.Completed ? "Workflow completed successfully" : run.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed for project {ProjectId}", projectId);
            run.Status = ProjectStatus.Failed;
            run.ErrorMessage = ex.Message;
            run.CompletedAt = DateTime.UtcNow;
            
            OnWorkflowStateChanged(projectId, run.Id, ProjectStatus.Failed, ex.Message);
        }
        finally
        {
            _runningWorkflows.TryRemove(run.Id, out _);
            cts.Dispose();
        }

        return run;
    }

    public async Task<StageResult> ExecuteStageAsync(Guid projectId, WorkflowStage stage, Dictionary<string, object> context, CancellationToken cancellationToken = default)
    {
        var result = new StageResult
        {
            StageName = stage.Name,
            StartedAt = DateTime.UtcNow,
            Status = StageStatus.Running
        };

        try
        {
            OnStageStateChanged(projectId, (Guid)context["runId"], stage.Name, StageStatus.Running, null, null);

            // Check condition if present
            if (stage.Condition != null)
            {
                var shouldExecute = await EvaluateConditionAsync(stage.Condition, context);
                if (!shouldExecute)
                {
                    result.Status = StageStatus.Skipped;
                    result.CompletedAt = DateTime.UtcNow;
                    
                    OnStageStateChanged(projectId, (Guid)context["runId"], stage.Name, StageStatus.Skipped, null, null);
                    return result;
                }
            }

            // Execute the agent for this stage
            var agentRequest = new AgentRequest
            {
                Command = stage.AgentType,
                Parameters = stage.Parameters,
                Context = new AgentContext 
                { 
                    ProjectId = projectId.ToString(),
                    SharedState = context
                }
            };

            var agentResult = await _agentOrchestrator.ExecuteAsync(agentRequest, cancellationToken);
            
            result.AgentId = agentResult.AgentId.ToString();
            result.Output = agentResult.Output ?? new Dictionary<string, object>();
            result.Status = agentResult.Success ? StageStatus.Completed : StageStatus.Failed;
            result.ErrorMessage = agentResult.Error;
            result.CompletedAt = DateTime.UtcNow;

            // Track costs if applicable
            if (agentResult.TokensUsed > 0)
            {
                var project = await _projectService.GetProjectAsync(projectId, cancellationToken);
                if (project?.Configuration.ProviderId != null)
                {
                    var cost = await _costTracker.CalculateCostAsync(
                        project.Configuration.ProviderId,
                        project.Configuration.Agents.DefaultModel,
                        agentResult.TokensUsed,
                        agentResult.TokensGenerated);

                    await _costTracker.RecordCostAsync(projectId, (Guid)context["runId"], cost, cancellationToken);
                }
            }

            OnStageStateChanged(projectId, (Guid)context["runId"], stage.Name, result.Status, result.Output, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stage {StageName} execution failed for project {ProjectId}", stage.Name, projectId);
            result.Status = StageStatus.Failed;
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
            
            OnStageStateChanged(projectId, (Guid)context["runId"], stage.Name, StageStatus.Failed, null, ex.Message);
        }

        return result;
    }

    public Task<bool> EvaluateConditionAsync(WorkflowCondition condition, Dictionary<string, object> context)
    {
        // Simple condition evaluation - can be extended with expression engine
        var result = condition.Type.ToLower() switch
        {
            "if" => EvaluateExpression(condition.Expression, context, true),
            "unless" => !EvaluateExpression(condition.Expression, context, false),
            "when" => EvaluateWhenCondition(condition.Expression, context),
            _ => true
        };

        return Task.FromResult(result);
    }

    public Task PauseWorkflowAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (_runningWorkflows.TryGetValue(runId, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Paused workflow run {RunId}", runId);
        }
        
        return Task.CompletedTask;
    }

    public Task ResumeWorkflowAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would restore workflow state and continue execution
        _logger.LogInformation("Resume requested for workflow run {RunId}", runId);
        return Task.CompletedTask;
    }

    public Task CancelWorkflowAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (_runningWorkflows.TryRemove(runId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _logger.LogInformation("Cancelled workflow run {RunId}", runId);
        }
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<WorkflowStage>> GetExecutionPlanAsync(WorkflowConfiguration workflow, Dictionary<string, object> context)
    {
        var plan = new List<WorkflowStage>();
        var executed = new HashSet<string>();

        // Simple dependency resolution - can be enhanced with topological sort
        while (plan.Count < workflow.Stages.Count)
        {
            var added = false;
            
            foreach (var stage in workflow.Stages)
            {
                if (executed.Contains(stage.Name))
                    continue;

                if (stage.DependsOn.All(dep => executed.Contains(dep)))
                {
                    plan.Add(stage);
                    executed.Add(stage.Name);
                    added = true;
                }
            }

            if (!added && plan.Count < workflow.Stages.Count)
            {
                // Circular dependency or missing dependency
                _logger.LogWarning("Workflow has unresolvable dependencies");
                break;
            }
        }

        return Task.FromResult(plan.AsEnumerable());
    }

    public Task<bool> ValidateWorkflowAsync(WorkflowConfiguration workflow, out List<string> errors)
    {
        errors = new List<string>();

        if (workflow.Stages == null || workflow.Stages.Count == 0)
        {
            errors.Add("Workflow must have at least one stage");
        }

        var stageNames = new HashSet<string>();
        foreach (var stage in workflow.Stages ?? new())
        {
            if (string.IsNullOrEmpty(stage.Name))
            {
                errors.Add("All stages must have a name");
            }
            else if (!stageNames.Add(stage.Name))
            {
                errors.Add($"Duplicate stage name: {stage.Name}");
            }

            if (string.IsNullOrEmpty(stage.AgentType))
            {
                errors.Add($"Stage {stage.Name} must have an agent type");
            }

            foreach (var dep in stage.DependsOn)
            {
                if (workflow.Stages?.Any(s => s.Name == dep) != true)
                {
                    errors.Add($"Stage {stage.Name} depends on unknown stage {dep}");
                }
            }
        }

        // Check for circular dependencies
        if (errors.Count == 0 && workflow.Stages != null)
        {
            var plan = GetExecutionPlanAsync(workflow, new Dictionary<string, object>()).Result;
            if (plan.Count() != workflow.Stages.Count)
            {
                errors.Add("Workflow contains circular dependencies");
            }
        }

        return Task.FromResult(errors.Count == 0);
    }

    private bool EvaluateExpression(string expression, Dictionary<string, object> context, bool defaultValue)
    {
        // Simple expression evaluation - can be replaced with proper expression engine
        if (context.TryGetValue(expression, out var value))
        {
            return value switch
            {
                bool b => b,
                string s => !string.IsNullOrEmpty(s),
                int i => i > 0,
                _ => defaultValue
            };
        }

        return defaultValue;
    }

    private bool EvaluateWhenCondition(string expression, Dictionary<string, object> context)
    {
        // Evaluate "when" conditions like "stage.previous.success"
        var parts = expression.Split('.');
        if (parts.Length >= 3 && parts[0] == "stage")
        {
            var stageName = parts[1];
            var property = parts[2];
            
            var key = $"stage.{stageName}.status";
            if (context.TryGetValue(key, out var status) && status is StageStatus stageStatus)
            {
                return property.ToLower() switch
                {
                    "success" => stageStatus == StageStatus.Completed,
                    "failed" => stageStatus == StageStatus.Failed,
                    "skipped" => stageStatus == StageStatus.Skipped,
                    _ => false
                };
            }
        }

        return true;
    }

    private void OnWorkflowStateChanged(Guid projectId, Guid runId, ProjectStatus status, string? message)
    {
        WorkflowStateChanged?.Invoke(this, new WorkflowEventArgs
        {
            ProjectId = projectId,
            RunId = runId,
            Status = status,
            Message = message
        });
    }

    private void OnStageStateChanged(Guid projectId, Guid runId, string stageName, StageStatus status, Dictionary<string, object>? output, string? errorMessage)
    {
        StageStateChanged?.Invoke(this, new StageEventArgs
        {
            ProjectId = projectId,
            RunId = runId,
            StageName = stageName,
            Status = status,
            Output = output,
            ErrorMessage = errorMessage
        });
    }
}