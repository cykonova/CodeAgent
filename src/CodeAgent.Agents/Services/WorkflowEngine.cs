using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Agents.Services;

public class WorkflowEngine : IWorkflowEngine
{
    private readonly Dictionary<string, WorkflowDefinition> _workflows = new();
    private readonly ILogger<WorkflowEngine>                _logger;

    public WorkflowEngine(ILogger<WorkflowEngine> logger)
    {
        _logger = logger;
        InitializeDefaultWorkflows();
    }

    public Task<WorkflowDefinition?> DetermineWorkflowAsync(OrchestratorRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Determining workflow for command: {Command}", request.Command);
        
        var workflow = AnalyzeCommand(request.Command);
        
        if (workflow != null)
        {
            _logger.LogInformation("Selected workflow: {WorkflowName}", workflow.Name);
        }
        
        return Task.FromResult(workflow);
    }

    public Task<WorkflowDefinition?> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        _workflows.TryGetValue(workflowId, out var workflow);
        return Task.FromResult(workflow);
    }

    public Task RegisterWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        _workflows[workflow.Id] = workflow;
        _logger.LogInformation("Registered workflow: {WorkflowId} - {WorkflowName}", workflow.Id, workflow.Name);
        return Task.CompletedTask;
    }

    private WorkflowDefinition? AnalyzeCommand(string command)
    {
        var lowerCommand = command.ToLowerInvariant();
        
        if (ContainsKeywords(lowerCommand, "implement", "create", "build", "develop", "add feature"))
        {
            return CreateImplementationWorkflow();
        }
        
        if (ContainsKeywords(lowerCommand, "review", "analyze", "check", "audit"))
        {
            return CreateReviewWorkflow();
        }
        
        if (ContainsKeywords(lowerCommand, "test", "verify", "validate"))
        {
            return CreateTestingWorkflow();
        }
        
        if (ContainsKeywords(lowerCommand, "document", "explain", "describe"))
        {
            return CreateDocumentationWorkflow();
        }
        
        if (ContainsKeywords(lowerCommand, "refactor", "optimize", "improve"))
        {
            return CreateRefactoringWorkflow();
        }
        
        return CreateDefaultWorkflow();
    }

    private bool ContainsKeywords(string text, params string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword));
    }

    private void InitializeDefaultWorkflows()
    {
        var workflows = new[]
        {
            CreateImplementationWorkflow(),
            CreateReviewWorkflow(),
            CreateTestingWorkflow(),
            CreateDocumentationWorkflow(),
            CreateRefactoringWorkflow(),
            CreateDefaultWorkflow()
        };

        foreach (var workflow in workflows)
        {
            _workflows[workflow.Id] = workflow;
        }
    }

    private WorkflowDefinition CreateImplementationWorkflow()
    {
        return new WorkflowDefinition
        {
            Id = "implementation",
            Name = "Implementation Workflow",
            Description = "Full implementation workflow with planning, coding, testing, and documentation",
            Steps = new List<WorkflowStep>
            {
                new()
                {
                    Name = "Planning",
                    AgentType = AgentType.Planning,
                    Command = "analyze_requirements",
                    IsRequired = true,
                    Order = 1
                },
                new()
                {
                    Name = "Architecture Design",
                    AgentType = AgentType.Planning,
                    Command = "design_architecture",
                    IsRequired = true,
                    Order = 2
                },
                new()
                {
                    Name = "Implementation",
                    AgentType = AgentType.Coding,
                    Command = "implement_feature",
                    IsRequired = true,
                    Order = 3
                },
                new()
                {
                    Name = "Code Review",
                    AgentType = AgentType.Review,
                    Command = "review_code",
                    IsRequired = false,
                    Order = 4
                },
                new()
                {
                    Name = "Testing",
                    AgentType = AgentType.Testing,
                    Command = "generate_tests",
                    IsRequired = true,
                    Order = 5
                },
                new()
                {
                    Name = "Documentation",
                    AgentType = AgentType.Documentation,
                    Command = "generate_documentation",
                    IsRequired = false,
                    Order = 6
                }
            }
        };
    }

    private WorkflowDefinition CreateReviewWorkflow()
    {
        return new WorkflowDefinition
        {
            Id = "review",
            Name = "Code Review Workflow",
            Description = "Code review and quality analysis",
            Steps = new List<WorkflowStep>
            {
                new()
                {
                    Name = "Code Analysis",
                    AgentType = AgentType.Review,
                    Command = "analyze_code",
                    IsRequired = true,
                    Order = 1
                },
                new()
                {
                    Name = "Security Review",
                    AgentType = AgentType.Review,
                    Command = "security_review",
                    IsRequired = true,
                    Order = 2
                },
                new()
                {
                    Name = "Recommendations",
                    AgentType = AgentType.Review,
                    Command = "generate_recommendations",
                    IsRequired = true,
                    Order = 3
                }
            }
        };
    }

    private WorkflowDefinition CreateTestingWorkflow()
    {
        return new WorkflowDefinition
        {
            Id = "testing",
            Name = "Testing Workflow",
            Description = "Comprehensive testing workflow",
            Steps = new List<WorkflowStep>
            {
                new()
                {
                    Name = "Test Planning",
                    AgentType = AgentType.Testing,
                    Command = "plan_tests",
                    IsRequired = true,
                    Order = 1
                },
                new()
                {
                    Name = "Unit Tests",
                    AgentType = AgentType.Testing,
                    Command = "generate_unit_tests",
                    IsRequired = true,
                    Order = 2
                },
                new()
                {
                    Name = "Integration Tests",
                    AgentType = AgentType.Testing,
                    Command = "generate_integration_tests",
                    IsRequired = false,
                    Order = 3
                }
            }
        };
    }

    private WorkflowDefinition CreateDocumentationWorkflow()
    {
        return new WorkflowDefinition
        {
            Id = "documentation",
            Name = "Documentation Workflow",
            Description = "Documentation generation workflow",
            Steps = new List<WorkflowStep>
            {
                new()
                {
                    Name = "Code Analysis",
                    AgentType = AgentType.Documentation,
                    Command = "analyze_code_structure",
                    IsRequired = true,
                    Order = 1
                },
                new()
                {
                    Name = "API Documentation",
                    AgentType = AgentType.Documentation,
                    Command = "generate_api_docs",
                    IsRequired = true,
                    Order = 2
                },
                new()
                {
                    Name = "User Guide",
                    AgentType = AgentType.Documentation,
                    Command = "generate_user_guide",
                    IsRequired = false,
                    Order = 3
                }
            }
        };
    }

    private WorkflowDefinition CreateRefactoringWorkflow()
    {
        return new WorkflowDefinition
        {
            Id = "refactoring",
            Name = "Refactoring Workflow",
            Description = "Code refactoring and optimization",
            Steps = new List<WorkflowStep>
            {
                new()
                {
                    Name = "Code Analysis",
                    AgentType = AgentType.Review,
                    Command = "analyze_for_refactoring",
                    IsRequired = true,
                    Order = 1
                },
                new()
                {
                    Name = "Refactoring Plan",
                    AgentType = AgentType.Planning,
                    Command = "plan_refactoring",
                    IsRequired = true,
                    Order = 2
                },
                new()
                {
                    Name = "Implementation",
                    AgentType = AgentType.Coding,
                    Command = "implement_refactoring",
                    IsRequired = true,
                    Order = 3
                },
                new()
                {
                    Name = "Testing",
                    AgentType = AgentType.Testing,
                    Command = "verify_refactoring",
                    IsRequired = true,
                    Order = 4
                }
            }
        };
    }

    private WorkflowDefinition CreateDefaultWorkflow()
    {
        return new WorkflowDefinition
        {
            Id = "default",
            Name = "Default Workflow",
            Description = "Simple single-agent workflow",
            Steps = new List<WorkflowStep>
            {
                new()
                {
                    Name = "Process",
                    AgentType = AgentType.Coding,
                    Command = "process_request",
                    IsRequired = true,
                    Order = 1
                }
            }
        };
    }
}