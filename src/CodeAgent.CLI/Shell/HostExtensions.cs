using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

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

        builder.ConfigureServices(collection =>
        {
            collection.AddSingleton(settings);
        });

        var app = new CommandApp(registrar);

        app.Configure(conf =>
        {
            conf.SetApplicationName(applicationName);
            configurator?.Invoke(conf);
        });

        // Build the host to get services
        var host = builder.Build();
        var serviceProvider = host.Services;

        // Create and run the interactive shell
        var shell = new InteractiveShell(
            settings.ShellPrompt,
            settings.History,
            app,
            serviceProvider,
            settings);

        var result = await shell.ShowAsync(Spectre.Console.AnsiConsole.Console, CancellationToken.None);

        // Save history
        await File.WriteAllLinesAsync(historyFilePath, settings.History.AsEnumerable().Reverse());

        return result;
    }
}