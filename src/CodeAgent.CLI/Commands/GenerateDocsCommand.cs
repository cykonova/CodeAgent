using System.ComponentModel;
using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class GenerateDocsCommand : AsyncCommand<GenerateDocsCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IChatService _chatService;

    public GenerateDocsCommand(IFileSystemService fileSystemService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("Directory to generate documentation for")]
        [CommandArgument(0, "[directory]")]
        public string Directory { get; set; } = ".";

        [Description("Output directory for documentation")]
        [CommandOption("-o|--output")]
        public string OutputDirectory { get; set; } = "docs";

        [Description("Documentation format (markdown, html)")]
        [CommandOption("-f|--format")]
        public DocFormat Format { get; set; } = DocFormat.Markdown;

        [Description("Include private members")]
        [CommandOption("--include-private")]
        public bool IncludePrivate { get; set; }

        [Description("Generate API reference")]
        [CommandOption("--api")]
        public bool GenerateApiReference { get; set; } = true;

        [Description("Generate architecture overview")]
        [CommandOption("--architecture")]
        public bool GenerateArchitecture { get; set; } = true;
    }

    public enum DocFormat
    {
        Markdown,
        Html
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold cyan]Generating Documentation[/]"));
            
            // Get all source files
            var files = await _fileSystemService.GetFilesAsync(settings.Directory, "*.cs", true);
            files = files.Concat(await _fileSystemService.GetFilesAsync(settings.Directory, "*.ts", true)).ToArray();
            files = files.Concat(await _fileSystemService.GetFilesAsync(settings.Directory, "*.js", true)).ToArray();
            files = files.Concat(await _fileSystemService.GetFilesAsync(settings.Directory, "*.py", true)).ToArray();
            
            if (!files.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No source files found in the specified directory.[/]");
                return 0;
            }
            
            AnsiConsole.MarkupLine($"[cyan]Found {files.Length} source files to document[/]");
            
            // Create output directory
            await _fileSystemService.CreateDirectoryAsync(settings.OutputDirectory);
            
            var documentation = new List<(string fileName, string content)>();
            
            // Generate architecture overview if requested
            if (settings.GenerateArchitecture)
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .StartAsync("Analyzing project architecture...", async ctx =>
                    {
                        var architectureDoc = await GenerateArchitectureOverview(files, settings);
                        documentation.Add(("architecture.md", architectureDoc));
                    });
            }
            
            // Generate API documentation if requested
            if (settings.GenerateApiReference)
            {
                await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Generating API documentation...[/]", maxValue: files.Length);
                        
                        foreach (var file in files)
                        {
                            var relativeFilePath = Path.GetRelativePath(settings.Directory, file);
                            task.Description = $"[green]Documenting: {relativeFilePath}[/]";
                            
                            var fileDoc = await GenerateFileDocumentation(file, settings);
                            if (!string.IsNullOrWhiteSpace(fileDoc))
                            {
                                var docFileName = Path.ChangeExtension(relativeFilePath.Replace(Path.DirectorySeparatorChar, '_'), ".md");
                                documentation.Add((docFileName, fileDoc));
                            }
                            
                            task.Increment(1);
                        }
                    });
            }
            
            // Generate index/table of contents
            var indexContent = GenerateIndex(documentation, settings);
            documentation.Insert(0, ("index.md", indexContent));
            
            // Write all documentation files
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync("Writing documentation files...", async ctx =>
                {
                    foreach (var (fileName, content) in documentation)
                    {
                        var outputPath = Path.Combine(settings.OutputDirectory, fileName);
                        await _fileSystemService.WriteFileAsync(outputPath, content);
                    }
                });
            
            // Display summary
            var panel = new Panel($"[green]✓ Documentation generated successfully![/]\n" +
                                $"Files created: {documentation.Count}\n" +
                                $"Output directory: {settings.OutputDirectory}")
            {
                Header = new PanelHeader(" Documentation Summary "),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<string> GenerateArchitectureOverview(string[] files, Settings settings)
    {
        // Analyze project structure
        var projectStructure = AnalyzeProjectStructure(files);
        
        var prompt = $"Generate a comprehensive architecture documentation for this project:\n\n" +
                    $"Project Structure:\n{projectStructure}\n\n" +
                    "Include:\n" +
                    "1. High-level architecture overview\n" +
                    "2. Component descriptions\n" +
                    "3. Data flow diagrams (as ASCII art or Mermaid)\n" +
                    "4. Key design patterns used\n" +
                    "5. Dependencies and external integrations\n" +
                    "Format as Markdown with appropriate headers.";
        
        var result = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
        {
            result.Append(chunk);
        }
        
        return result.ToString();
    }

    private async Task<string> GenerateFileDocumentation(string filePath, Settings settings)
    {
        var content = await _fileSystemService.ReadFileAsync(filePath);
        
        // Skip if file is too small or likely auto-generated
        if (content.Length < 100 || content.Contains("auto-generated"))
        {
            return string.Empty;
        }
        
        var prompt = $"Generate API documentation for this file:\n" +
                    $"File: {Path.GetFileName(filePath)}\n\n" +
                    $"Content:\n{content}\n\n" +
                    "Generate documentation including:\n" +
                    "1. File purpose and overview\n" +
                    "2. Classes/interfaces/types with descriptions\n" +
                    "3. Public methods/functions with parameters and return types\n" +
                    "4. Usage examples where appropriate\n";
        
        if (!settings.IncludePrivate)
        {
            prompt += "5. Only document public members\n";
        }
        
        prompt += "Format as Markdown with code examples.";
        
        var result = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
        {
            result.Append(chunk);
        }
        
        return result.ToString();
    }

    private string AnalyzeProjectStructure(string[] files)
    {
        var structure = new StringBuilder();
        var directories = files.Select(Path.GetDirectoryName)
                              .Distinct()
                              .OrderBy(d => d)
                              .ToList();
        
        foreach (var dir in directories)
        {
            var filesInDir = files.Where(f => Path.GetDirectoryName(f) == dir).ToList();
            structure.AppendLine($"{dir}/");
            foreach (var file in filesInDir)
            {
                structure.AppendLine($"  - {Path.GetFileName(file)}");
            }
        }
        
        return structure.ToString();
    }

    private string GenerateIndex(List<(string fileName, string content)> documentation, Settings settings)
    {
        var index = new StringBuilder();
        index.AppendLine("# Project Documentation");
        index.AppendLine();
        index.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        index.AppendLine();
        
        if (documentation.Any(d => d.fileName == "architecture.md"))
        {
            index.AppendLine("## Architecture");
            index.AppendLine("- [Architecture Overview](architecture.md)");
            index.AppendLine();
        }
        
        index.AppendLine("## API Reference");
        foreach (var (fileName, _) in documentation.Where(d => d.fileName != "architecture.md"))
        {
            var displayName = Path.GetFileNameWithoutExtension(fileName).Replace('_', '/');
            index.AppendLine($"- [{displayName}]({fileName})");
        }
        
        return index.ToString();
    }
}