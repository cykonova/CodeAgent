using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class ScanCommand : AsyncCommand
{
    private readonly IServiceProvider _serviceProvider;

    public ScanCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var console = AnsiConsole.Console;
        var fileSystem = _serviceProvider.GetRequiredService<IFileSystemService>();
        
        console.MarkupLine("[yellow]Scanning project structure...[/]");
        
        var currentDir = Directory.GetCurrentDirectory();
        var files = await fileSystem.GetProjectFilesAsync(currentDir);
        
        var table = new Table();
        table.AddColumn("File Type");
        table.AddColumn("Count");
        
        var fileGroups = files.GroupBy(f => Path.GetExtension(f) ?? "no extension");
        foreach (var group in fileGroups.OrderBy(g => g.Key))
        {
            table.AddRow(group.Key, group.Count().ToString());
        }
        
        console.Write(table);
        console.MarkupLine($"[green]Total files: {files.Count()}[/]");
        
        return 0;
    }
}