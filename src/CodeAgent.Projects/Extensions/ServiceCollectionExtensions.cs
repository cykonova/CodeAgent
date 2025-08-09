using CodeAgent.Projects.Interfaces;
using CodeAgent.Projects.Services;
using CodeAgent.Projects.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace CodeAgent.Projects.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectManagement(this IServiceCollection services)
    {
        // Register template provider
        services.AddSingleton<ITemplateProvider, TemplateProvider>();

        // Register configuration manager
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();

        // Register project service
        services.AddSingleton<IProjectService, ProjectService>();

        // Register cost tracker
        services.AddSingleton<ICostTracker, CostTracker>();

        // Register workflow engine
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();

        return services;
    }
}