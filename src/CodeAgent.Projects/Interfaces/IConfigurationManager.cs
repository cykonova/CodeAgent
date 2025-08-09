using CodeAgent.Projects.Models;

namespace CodeAgent.Projects.Interfaces;

public interface IConfigurationManager
{
    ProjectConfiguration GetEffectiveConfiguration(Guid projectId);
    ProjectConfiguration MergeConfigurations(params ProjectConfiguration?[] configurations);
    ProjectConfiguration ApplyTemplate(ProjectConfiguration baseConfig, string templateName);
    ProjectConfiguration ApplyOverrides(ProjectConfiguration config, Dictionary<string, object> overrides);
    bool ValidateConfiguration(ProjectConfiguration configuration, out List<string> errors);
    ProjectConfiguration GetDefaultConfiguration();
    ProjectConfiguration GetSystemDefaults();
    ProjectConfiguration GetUserDefaults(string? userId = null);
    Task<ProjectConfiguration> LoadConfigurationAsync(string configPath, CancellationToken cancellationToken = default);
    Task SaveConfigurationAsync(ProjectConfiguration configuration, string configPath, CancellationToken cancellationToken = default);
}