namespace CodeAgent.Agents.Services;

public interface IServiceProvider
{
    object? GetService(Type serviceType);
}