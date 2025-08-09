using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeAgent.Sandbox.Configuration;
using CodeAgent.Sandbox.MCP;
using CodeAgent.Sandbox.Permissions;
using CodeAgent.Sandbox.Services;

namespace CodeAgent.Sandbox.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSandboxServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration
        services.Configure<SandboxOptions>(configuration.GetSection(SandboxOptions.SectionName));

        // Register core services
        services.AddSingleton<ISandboxManager, DockerSandboxManager>();
        services.AddSingleton<IPermissionProxy, PermissionProxy>();
        services.AddSingleton<IMcpServer, McpServer>();

        // Register as hosted service if needed for background operations
        services.AddHostedService<SandboxCleanupService>();

        return services;
    }
}