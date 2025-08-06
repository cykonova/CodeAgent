using CodeAgent.CLI.Shell;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class ShellCommand : AsyncCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandApp _commandApp;
    private readonly ShellSettings _settings;

    public ShellCommand(IServiceProvider serviceProvider, ICommandApp commandApp, ShellSettings settings)
    {
        _serviceProvider = serviceProvider;
        _commandApp = commandApp;
        _settings = settings;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var shell = new InteractiveShell(
            _settings.ShellPrompt,
            _settings.History,
            _commandApp,
            _serviceProvider,
            _settings);

        return await shell.ShowAsync(AnsiConsole.Console, CancellationToken.None);
    }
}