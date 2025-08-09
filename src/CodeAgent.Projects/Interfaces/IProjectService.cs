using CodeAgent.Projects.Models;

namespace CodeAgent.Projects.Interfaces;

public interface IProjectService
{
    Task<Project> CreateProjectAsync(string name, ProjectConfiguration? configuration = null, CancellationToken cancellationToken = default);
    Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetAllProjectsAsync(CancellationToken cancellationToken = default);
    Task<Project> UpdateProjectAsync(Guid projectId, ProjectConfiguration configuration, CancellationToken cancellationToken = default);
    Task<bool> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Project> CloneProjectAsync(Guid sourceProjectId, string newName, CancellationToken cancellationToken = default);
    Task<Project> ApplyTemplateAsync(Guid projectId, string templateName, CancellationToken cancellationToken = default);
    Task<ProjectState> GetProjectStateAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task UpdateProjectStateAsync(Guid projectId, ProjectState state, CancellationToken cancellationToken = default);
}