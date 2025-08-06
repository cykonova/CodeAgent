using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using CodeAgent.CLI.Commands;

namespace CodeAgent.CLI.Shell;

public static class HostExtensions
{
    public static async Task<int> RunInteractiveShell(
        this IHostBuilder builder,
        string applicationName,
        string historyFilePath,
        IEnumerable<string> args,
        Action<IConfigurator>? configurator = default)
    {
        if (builder is not ITypeRegistrar registrar)
        {
            throw new Exception("Builder must be of type CliHostBuilder");
        }

        var settings = new ShellSettings
        {
            ShellPrompt = "CodeAgent> ",
            History = !File.Exists(historyFilePath)
                ? new List<string>()
                : File.ReadLines(historyFilePath).Reverse().ToList(),
            HistoryFilePath = historyFilePath,
            EnableChat = true,
            CommandPrefix = "/"
        };

        // Register services before creating CommandApp
        builder.ConfigureServices(collection =>
        {
            collection.AddSingleton(settings);
            collection.AddSingleton<ShellCommand>();
        });

        // Create CommandApp with the registrar
        var app = new CommandApp(registrar);

        app.Configure(conf =>
        {
            conf.SetApplicationName(applicationName);
            configurator?.Invoke(conf);
        });

        // Register the CommandApp as a service after it's configured
        builder.ConfigureServices(collection =>
        {
            collection.AddSingleton<ICommandApp>(app);
        });

        // Set the shell command as default
        app.SetDefaultCommand<ShellCommand>();
        
        // Run the app
        var result = await app.RunAsync(args);

        // Save history
        await File.WriteAllLinesAsync(historyFilePath, settings.History.AsEnumerable().Reverse());

        return result;
    }
}