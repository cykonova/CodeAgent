using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;

namespace CodeAgent.CLI.Shell;

public class CliHostBuilder : HostBuilder, ITypeRegistrar, IHostBuilder, ITypeRegistrarFrontend
{
    private IHost? _host;
    private ITypeResolver? _resolver;

    public void Register(Type service, Type implementation)
    {
        ConfigureServices((_, container) => { container.AddSingleton(service, implementation); });
    }

    public void RegisterInstance(Type service, object implementation)
    {
        ConfigureServices((_, container) =>
        {
            if (service.IsAssignableTo(typeof(ICommandModel)))
            {
                container.AddSingleton(typeof(ICommandModel), implementation);
            }
            container.AddSingleton(service, implementation);
        });
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        ConfigureServices((_, container) => { container.AddSingleton(service, _ => factory()); });
    }

    public void Register<TService, TImplementation>() where TImplementation : TService
    {
        Register(typeof(TService), typeof(TImplementation));
    }

    public void RegisterInstance<TImplementation>(TImplementation instance)
    {
        ArgumentNullException.ThrowIfNull(instance, nameof(instance));
        
        ConfigureServices((_, container) =>
        {
            if (typeof(TImplementation).IsAssignableTo(typeof(ICommandModel)))
            {
                container.AddSingleton(typeof(ICommandModel), instance);
            }
            container.AddSingleton(typeof(TImplementation), instance);
        });
    }

    public void RegisterInstance<TService, TImplementation>(TImplementation instance) where TImplementation : TService
    {
        RegisterInstance(instance);
    }

    ITypeResolver ITypeRegistrar.Build()
    {
        return _resolver ??= new HostTypeResolver(Build());
    }

    IHost IHostBuilder.Build()
    {
        return _host ??= base.Build();
    }

    private class HostTypeResolver(IHost host) : ITypeResolver
    {
        public object? Resolve(Type? type)
        {
            if (type == null || type.IsAssignableTo(typeof(CommandSettings)))
            {
                return null;
            }

            return host.Services.GetService(type);
        }
    }
}