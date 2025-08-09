using CodeAgent.Projects.Models;

namespace CodeAgent.Projects.Templates;

public interface ITemplateProvider
{
    ProjectConfiguration? GetTemplate(string templateName);
    IEnumerable<string> GetAvailableTemplates();
    void RegisterTemplate(string name, ProjectConfiguration configuration);
    bool RemoveTemplate(string name);
}