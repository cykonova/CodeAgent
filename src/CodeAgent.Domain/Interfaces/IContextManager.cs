using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface IContextManager
{
    /// <summary>
    /// Builds context for a project
    /// </summary>
    Task<ProjectContext> BuildContextAsync(string projectPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets relevant files based on a query
    /// </summary>
    Task<IReadOnlyList<string>> GetRelevantFilesAsync(string query, int maxFiles = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates context with modified files
    /// </summary>
    Task UpdateContextAsync(ProjectContext context, IReadOnlyList<string> modifiedFiles, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculates semantic similarity between query and files
    /// </summary>
    Task<Dictionary<string, double>> CalculateRelevanceScoresAsync(string query, IReadOnlyList<string> files, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current context
    /// </summary>
    ProjectContext? GetCurrentContext();
    
    /// <summary>
    /// Clears the current context
    /// </summary>
    void ClearContext();
}