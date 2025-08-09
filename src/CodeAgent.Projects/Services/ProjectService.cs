using CodeAgent.Projects.Interfaces;
using CodeAgent.Projects.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Projects.Services;

public class ProjectService : IProjectService
{
    private readonly ILogger<ProjectService> _logger;
    private readonly IConfigurationManager _configurationManager;
    private readonly Dictionary<Guid, Project> _projects = new();
    private readonly Dictionary<Guid, ProjectState> _projectStates = new();

    public ProjectService(ILogger<ProjectService> logger, IConfigurationManager configurationManager)
    {
        _logger = logger;
        _configurationManager = configurationManager;
    }

    public Task<Project> CreateProjectAsync(string name, ProjectConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        var project = new Project
        {
            Name = name,
            Configuration = configuration ?? _configurationManager.GetDefaultConfiguration(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _projects[project.Id] = project;
        _projectStates[project.Id] = new ProjectState();

        _logger.LogInformation("Created project {ProjectName} with ID {ProjectId}", name, project.Id);
        return Task.FromResult(project);
    }

    public Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        _projects.TryGetValue(projectId, out var project);
        return Task.FromResult(project);
    }

    public Task<IEnumerable<Project>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_projects.Values.AsEnumerable());
    }

    public Task<Project> UpdateProjectAsync(Guid projectId, ProjectConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!_projects.TryGetValue(projectId, out var project))
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        project.Configuration = configuration;
        project.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Updated project {ProjectId} configuration", projectId);
        return Task.FromResult(project);
    }

    public Task<bool> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var removed = _projects.Remove(projectId);
        _projectStates.Remove(projectId);

        if (removed)
        {
            _logger.LogInformation("Deleted project {ProjectId}", projectId);
        }

        return Task.FromResult(removed);
    }

    public async Task<Project> CloneProjectAsync(Guid sourceProjectId, string newName, CancellationToken cancellationToken = default)
    {
        var sourceProject = await GetProjectAsync(sourceProjectId, cancellationToken);
        if (sourceProject == null)
        {
            throw new InvalidOperationException($"Source project {sourceProjectId} not found");
        }

        var clonedProject = new Project
        {
            Name = newName,
            Description = sourceProject.Description,
            Type = sourceProject.Type,
            TemplateName = sourceProject.TemplateName,
            Configuration = sourceProject.Configuration,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>(sourceProject.Metadata)
        };

        _projects[clonedProject.Id] = clonedProject;
        _projectStates[clonedProject.Id] = new ProjectState();

        _logger.LogInformation("Cloned project {SourceId} to new project {NewId} with name {NewName}", 
            sourceProjectId, clonedProject.Id, newName);

        return clonedProject;
    }

    public async Task<Project> ApplyTemplateAsync(Guid projectId, string templateName, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId, cancellationToken);
        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        project.Configuration = _configurationManager.ApplyTemplate(project.Configuration, templateName);
        project.TemplateName = templateName;
        project.Type = templateName switch
        {
            "fast" => ProjectType.Fast,
            "quality" => ProjectType.Quality,
            "budget" => ProjectType.Budget,
            _ => ProjectType.Standard
        };
        project.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Applied template {TemplateName} to project {ProjectId}", templateName, projectId);
        return project;
    }

    public Task<ProjectState> GetProjectStateAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (!_projectStates.TryGetValue(projectId, out var state))
        {
            state = new ProjectState();
            _projectStates[projectId] = state;
        }

        return Task.FromResult(state);
    }

    public Task UpdateProjectStateAsync(Guid projectId, ProjectState state, CancellationToken cancellationToken = default)
    {
        _projectStates[projectId] = state;
        _logger.LogDebug("Updated state for project {ProjectId}", projectId);
        return Task.CompletedTask;
    }
}