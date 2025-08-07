using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace CodeAgent.CLI;

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceProvider _provider;

    public TypeRegistrar(IServiceProvider provider)
    {
        _provider = provider;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_provider);
    }

    public void Register(Type service, Type implementation)
    {
        // Not needed for our use case since we're using pre-built ServiceProvider
    }

    public void RegisterInstance(Type service, object implementation)
    {
        // Not needed for our use case since we're using pre-built ServiceProvider
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        // Not needed for our use case since we're using pre-built ServiceProvider
    }
}

public sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type)
    {
        if (type == null)
            return null;

        return _provider.GetService(type);
    }
}