using System.ComponentModel;
using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class ArchitectureCommand : AsyncCommand<ArchitectureCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IChatService _chatService;

    public ArchitectureCommand(IFileSystemService fileSystemService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("Project directory to analyze")]
        [CommandArgument(0, "[directory]")]
        public string Directory { get; set; } = ".";

        [Description("Output format (text, markdown, mermaid, json)")]
        [CommandOption("-f|--format")]
        public OutputFormat Format { get; set; } = OutputFormat.Markdown;

        [Description("Output file path")]
        [CommandOption("-o|--output")]
        public string? OutputPath { get; set; }

        [Description("Analysis depth (shallow, normal, deep)")]
        [CommandOption("-d|--depth")]
        public AnalysisDepth Depth { get; set; } = AnalysisDepth.Normal;

        [Description("Include dependency analysis")]
        [CommandOption("--dependencies")]
        public bool IncludeDependencies { get; set; } = true;

        [Description("Include design patterns detection")]
        [CommandOption("--patterns")]
        public bool IncludePatterns { get; set; } = true;

        [Description("Include metrics and statistics")]
        [CommandOption("--metrics")]
        public bool IncludeMetrics { get; set; } = true;

        [Description("Generate visual diagram")]
        [CommandOption("--diagram")]
        public bool GenerateDiagram { get; set; } = true;
    }

    public enum OutputFormat
    {
        Text,
        Markdown,
        Mermaid,
        Json
    }

    public enum AnalysisDepth
    {
        Shallow,
        Normal,
        Deep
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold cyan]Architecture Analysis[/]"));
            
            var architecture = new ArchitectureInfo();
            
            // Analyze project structure
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var scanTask = ctx.AddTask("[green]Scanning project structure...[/]");
                    architecture = await AnalyzeProjectStructure(settings.Directory, settings.Depth);
                    scanTask.Increment(25);
                    
                    if (settings.IncludeDependencies)
                    {
                        var depTask = ctx.AddTask("[green]Analyzing dependencies...[/]");
                        await AnalyzeDependencies(architecture, settings.Directory);
                        depTask.Increment(100);
                    }
                    
                    if (settings.IncludePatterns)
                    {
                        var patternTask = ctx.AddTask("[green]Detecting design patterns...[/]");
                        await DetectDesignPatterns(architecture, settings.Directory);
                        patternTask.Increment(100);
                    }
                    
                    if (settings.IncludeMetrics)
                    {
                        var metricsTask = ctx.AddTask("[green]Calculating metrics...[/]");
                        await CalculateMetrics(architecture, settings.Directory);
                        metricsTask.Increment(100);
                    }
                    
                    scanTask.Increment(75);
                });
            
            // Generate output
            string output = settings.Format switch
            {
                OutputFormat.Text => await GenerateTextOutput(architecture, settings),
                OutputFormat.Markdown => await GenerateMarkdownOutput(architecture, settings),
                OutputFormat.Mermaid => await GenerateMermaidOutput(architecture, settings),
                OutputFormat.Json => await GenerateJsonOutput(architecture),
                _ => throw new NotSupportedException($"Format {settings.Format} not supported")
            };
            
            // Write or display output
            if (!string.IsNullOrEmpty(settings.OutputPath))
            {
                await _fileSystemService.WriteFileAsync(settings.OutputPath, output);
                AnsiConsole.MarkupLine($"[green]✓ Architecture analysis saved to: {settings.OutputPath}[/]");
            }
            else
            {
                // Display in console
                if (settings.Format == OutputFormat.Markdown)
                {
                    var panel = new Panel(new Markup(output))
                    {
                        Header = new PanelHeader(" Architecture Analysis "),
                        Border = BoxBorder.Rounded
                    };
                    AnsiConsole.Write(panel);
                }
                else
                {
                    AnsiConsole.WriteLine(output);
                }
            }
            
            // Display summary
            DisplaySummary(architecture);
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<ArchitectureInfo> AnalyzeProjectStructure(string directory, AnalysisDepth depth)
    {
        var info = new ArchitectureInfo
        {
            RootDirectory = directory,
            ProjectName = Path.GetFileName(Path.GetFullPath(directory))
        };
        
        // Identify project type and structure
        await IdentifyProjectType(info, directory);
        
        // Map directory structure
        info.Layers = await MapProjectLayers(directory, depth);
        
        // Identify key components
        info.Components = await IdentifyComponents(directory, depth);
        
        return info;
    }

    private async Task IdentifyProjectType(ArchitectureInfo info, string directory)
    {
        // Check for .NET project
        if ((await _fileSystemService.GetFilesAsync(directory, "*.sln", false)).Any())
        {
            info.ProjectType = "NET Solution";
            info.Framework = ".NET";
        }
        else if ((await _fileSystemService.GetFilesAsync(directory, "*.csproj", true)).Any())
        {
            info.ProjectType = ".NET Project";
            info.Framework = ".NET";
        }
        // Check for Node.js
        else if (await _fileSystemService.FileExistsAsync(Path.Combine(directory, "package.json")))
        {
            info.ProjectType = "Node.js";
            var packageJson = await _fileSystemService.ReadFileAsync(Path.Combine(directory, "package.json"));
            info.Framework = packageJson.Contains("\"react\"") ? "React" :
                           packageJson.Contains("\"angular\"") ? "Angular" :
                           packageJson.Contains("\"vue\"") ? "Vue" : "Node.js";
        }
        // Check for Python
        else if (await _fileSystemService.FileExistsAsync(Path.Combine(directory, "setup.py")) ||
                await _fileSystemService.FileExistsAsync(Path.Combine(directory, "pyproject.toml")))
        {
            info.ProjectType = "Python";
            info.Framework = "Python";
        }
        else
        {
            info.ProjectType = "Unknown";
            info.Framework = "Unknown";
        }
    }

    private async Task<List<Layer>> MapProjectLayers(string directory, AnalysisDepth depth)
    {
        var layers = new List<Layer>();
        var dirs = await GetDirectoriesRecursive(directory, depth == AnalysisDepth.Deep ? 5 : 3);
        
        foreach (var dir in dirs)
        {
            var dirName = Path.GetFileName(dir).ToLower();
            var layerType = DetermineLayerType(dirName, dir);
            
            if (layerType != LayerType.Unknown)
            {
                var layer = layers.FirstOrDefault(l => l.Type == layerType);
                if (layer == null)
                {
                    layer = new Layer { Type = layerType, Directories = new List<string>() };
                    layers.Add(layer);
                }
                layer.Directories.Add(dir);
            }
        }
        
        return layers;
    }

    private LayerType DetermineLayerType(string dirName, string fullPath)
    {
        // Common patterns for layer identification
        if (dirName.Contains("domain") || dirName.Contains("core") || dirName.Contains("entities"))
            return LayerType.Domain;
        if (dirName.Contains("application") || dirName.Contains("service") || dirName.Contains("usecase"))
            return LayerType.Application;
        if (dirName.Contains("infrastructure") || dirName.Contains("data") || dirName.Contains("repository"))
            return LayerType.Infrastructure;
        if (dirName.Contains("presentation") || dirName.Contains("ui") || dirName.Contains("view") || 
            dirName.Contains("controller") || dirName.Contains("api"))
            return LayerType.Presentation;
        if (dirName.Contains("test") || dirName.Contains("spec"))
            return LayerType.Test;
        
        return LayerType.Unknown;
    }

    private async Task<List<Component>> IdentifyComponents(string directory, AnalysisDepth depth)
    {
        var components = new List<Component>();
        var sourceFiles = new List<string>();
        
        // Get source files
        var extensions = new[] { "*.cs", "*.ts", "*.js", "*.py", "*.java" };
        foreach (var ext in extensions)
        {
            sourceFiles.AddRange(await _fileSystemService.GetFilesAsync(directory, ext, true));
        }
        
        // Analyze files to identify components
        foreach (var file in sourceFiles.Take(depth == AnalysisDepth.Deep ? 1000 : 100))
        {
            var component = await AnalyzeFileForComponent(file);
            if (component != null)
            {
                components.Add(component);
            }
        }
        
        return components;
    }

    private async Task<Component?> AnalyzeFileForComponent(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var content = await _fileSystemService.ReadFileAsync(filePath);
        
        // Simple heuristic-based component detection
        var component = new Component
        {
            Name = fileName,
            FilePath = filePath
        };
        
        // Detect component type based on naming and content
        if (fileName.EndsWith("Controller") || content.Contains("@Controller") || content.Contains("[ApiController]"))
            component.Type = ComponentType.Controller;
        else if (fileName.EndsWith("Service") || content.Contains("@Service") || content.Contains("IService"))
            component.Type = ComponentType.Service;
        else if (fileName.EndsWith("Repository") || content.Contains("@Repository") || content.Contains("IRepository"))
            component.Type = ComponentType.Repository;
        else if (fileName.EndsWith("Model") || fileName.EndsWith("Entity") || content.Contains("@Entity"))
            component.Type = ComponentType.Model;
        else if (fileName.EndsWith("ViewModel") || fileName.EndsWith("Dto"))
            component.Type = ComponentType.ViewModel;
        else if (fileName.EndsWith("Test") || fileName.EndsWith("Spec") || content.Contains("[Test]") || content.Contains("describe("))
            component.Type = ComponentType.Test;
        else
            return null;
        
        return component;
    }

    private async Task AnalyzeDependencies(ArchitectureInfo architecture, string directory)
    {
        architecture.Dependencies = new List<Dependency>();
        
        // For .NET projects
        var csprojFiles = await _fileSystemService.GetFilesAsync(directory, "*.csproj", true);
        foreach (var csproj in csprojFiles)
        {
            var content = await _fileSystemService.ReadFileAsync(csproj);
            // Extract package references (simplified)
            var packageRefs = System.Text.RegularExpressions.Regex.Matches(content, 
                @"<PackageReference Include=""([^""]+)"" Version=""([^""]+)""");
            foreach (System.Text.RegularExpressions.Match match in packageRefs)
            {
                architecture.Dependencies.Add(new Dependency
                {
                    Name = match.Groups[1].Value,
                    Version = match.Groups[2].Value,
                    Type = "NuGet"
                });
            }
        }
        
        // For Node.js projects
        var packageJsonPath = Path.Combine(directory, "package.json");
        if (await _fileSystemService.FileExistsAsync(packageJsonPath))
        {
            var content = await _fileSystemService.ReadFileAsync(packageJsonPath);
            // Simple extraction (would need proper JSON parsing in production)
            var deps = System.Text.RegularExpressions.Regex.Matches(content,
                @"""([^""]+)""\s*:\s*""([^""]+)""");
            foreach (System.Text.RegularExpressions.Match match in deps)
            {
                if (!match.Groups[1].Value.StartsWith("@") && !match.Groups[1].Value.Contains("script"))
                {
                    architecture.Dependencies.Add(new Dependency
                    {
                        Name = match.Groups[1].Value,
                        Version = match.Groups[2].Value,
                        Type = "npm"
                    });
                }
            }
        }
    }

    private async Task DetectDesignPatterns(ArchitectureInfo architecture, string directory)
    {
        architecture.Patterns = new List<string>();
        
        var prompt = $"Analyze the following project structure and identify design patterns used:\n\n" +
                    $"Project Type: {architecture.ProjectType}\n" +
                    $"Framework: {architecture.Framework}\n" +
                    $"Layers found: {string.Join(", ", architecture.Layers.Select(l => l.Type))}\n" +
                    $"Component types: {string.Join(", ", architecture.Components.Select(c => c.Type).Distinct())}\n\n" +
                    "Identify architectural patterns (e.g., MVC, MVVM, Clean Architecture, Hexagonal, etc.) " +
                    "and design patterns (e.g., Repository, Factory, Observer, etc.). " +
                    "Return as a comma-separated list.";
        
        var result = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
        {
            result.Append(chunk);
        }
        
        architecture.Patterns = result.ToString().Split(',').Select(p => p.Trim()).ToList();
    }

    private async Task CalculateMetrics(ArchitectureInfo architecture, string directory)
    {
        architecture.Metrics = new Metrics();
        
        // Count files by type
        var sourceExtensions = new[] { "*.cs", "*.ts", "*.js", "*.py", "*.java", "*.go", "*.rs" };
        foreach (var ext in sourceExtensions)
        {
            var files = await _fileSystemService.GetFilesAsync(directory, ext, true);
            architecture.Metrics.TotalFiles += files.Length;
            
            // Count lines of code (simplified)
            foreach (var file in files)
            {
                var content = await _fileSystemService.ReadFileAsync(file);
                architecture.Metrics.TotalLinesOfCode += content.Split('\n').Length;
            }
        }
        
        // Count by component type
        architecture.Metrics.ComponentCounts = architecture.Components
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());
        
        // Count layers
        architecture.Metrics.LayerCount = architecture.Layers.Count;
        
        // Count dependencies
        architecture.Metrics.DependencyCount = architecture.Dependencies?.Count ?? 0;
    }

    private async Task<string> GenerateMarkdownOutput(ArchitectureInfo architecture, Settings settings)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Generate a comprehensive architecture documentation in Markdown format based on this analysis:");
        prompt.AppendLine();
        prompt.AppendLine($"# {architecture.ProjectName} Architecture");
        prompt.AppendLine();
        prompt.AppendLine($"**Project Type:** {architecture.ProjectType}");
        prompt.AppendLine($"**Framework:** {architecture.Framework}");
        prompt.AppendLine();
        
        if (architecture.Layers.Any())
        {
            prompt.AppendLine("## Architectural Layers");
            foreach (var layer in architecture.Layers)
            {
                prompt.AppendLine($"- **{layer.Type}**: {layer.Directories.Count} directories");
            }
        }
        
        if (architecture.Patterns?.Any() == true)
        {
            prompt.AppendLine("\n## Design Patterns Detected");
            prompt.AppendLine(string.Join(", ", architecture.Patterns));
        }
        
        if (settings.IncludeMetrics && architecture.Metrics != null)
        {
            prompt.AppendLine("\n## Metrics");
            prompt.AppendLine($"- Total Files: {architecture.Metrics.TotalFiles}");
            prompt.AppendLine($"- Lines of Code: {architecture.Metrics.TotalLinesOfCode:N0}");
            prompt.AppendLine($"- Layers: {architecture.Metrics.LayerCount}");
            prompt.AppendLine($"- Dependencies: {architecture.Metrics.DependencyCount}");
        }
        
        if (settings.GenerateDiagram)
        {
            prompt.AppendLine("\nGenerate a Mermaid diagram showing the architecture.");
        }
        
        prompt.AppendLine("\nProvide:");
        prompt.AppendLine("1. Detailed architecture overview");
        prompt.AppendLine("2. Component interaction descriptions");
        prompt.AppendLine("3. Data flow explanations");
        prompt.AppendLine("4. Key architectural decisions and rationale");
        prompt.AppendLine("5. Recommendations for improvements");
        
        var result = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt.ToString()))
        {
            result.Append(chunk);
        }
        
        return result.ToString();
    }

    private Task<string> GenerateTextOutput(ArchitectureInfo architecture, Settings settings)
    {
        var output = new StringBuilder();
        output.AppendLine($"Architecture Analysis: {architecture.ProjectName}");
        output.AppendLine(new string('=', 50));
        output.AppendLine($"Project Type: {architecture.ProjectType}");
        output.AppendLine($"Framework: {architecture.Framework}");
        output.AppendLine();
        
        if (architecture.Layers.Any())
        {
            output.AppendLine("Layers:");
            foreach (var layer in architecture.Layers)
            {
                output.AppendLine($"  - {layer.Type}: {layer.Directories.Count} directories");
            }
        }
        
        if (architecture.Metrics != null)
        {
            output.AppendLine("\nMetrics:");
            output.AppendLine($"  Files: {architecture.Metrics.TotalFiles}");
            output.AppendLine($"  Lines: {architecture.Metrics.TotalLinesOfCode:N0}");
            output.AppendLine($"  Dependencies: {architecture.Metrics.DependencyCount}");
        }
        
        return Task.FromResult(output.ToString());
    }

    private async Task<string> GenerateMermaidOutput(ArchitectureInfo architecture, Settings settings)
    {
        var prompt = $"Generate a Mermaid diagram for this architecture:\n" +
                    $"Project: {architecture.ProjectName}\n" +
                    $"Type: {architecture.ProjectType}\n" +
                    $"Layers: {string.Join(", ", architecture.Layers.Select(l => l.Type))}\n" +
                    $"Components: {string.Join(", ", architecture.Components.Select(c => c.Type).Distinct())}\n\n" +
                    "Create a comprehensive architecture diagram showing layers, components, and relationships.";
        
        var result = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
        {
            result.Append(chunk);
        }
        
        return result.ToString();
    }

    private Task<string> GenerateJsonOutput(ArchitectureInfo architecture)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(architecture, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        return Task.FromResult(json);
    }

    private void DisplaySummary(ArchitectureInfo architecture)
    {
        var table = new Table();
        table.AddColumn("Metric");
        table.AddColumn("Value");
        table.Border(TableBorder.Rounded);
        
        table.AddRow("Project Type", architecture.ProjectType);
        table.AddRow("Framework", architecture.Framework);
        table.AddRow("Layers", architecture.Layers.Count.ToString());
        table.AddRow("Components", architecture.Components.Count.ToString());
        
        if (architecture.Metrics != null)
        {
            table.AddRow("Total Files", architecture.Metrics.TotalFiles.ToString());
            table.AddRow("Lines of Code", architecture.Metrics.TotalLinesOfCode.ToString("N0"));
            table.AddRow("Dependencies", architecture.Metrics.DependencyCount.ToString());
        }
        
        AnsiConsole.Write(table);
    }

    private async Task<List<string>> GetDirectoriesRecursive(string path, int maxDepth, int currentDepth = 0)
    {
        var directories = new List<string>();
        
        if (currentDepth >= maxDepth)
            return directories;
        
        try
        {
            var dirs = Directory.GetDirectories(path)
                .Where(d => !Path.GetFileName(d).StartsWith(".") && 
                           !Path.GetFileName(d).Equals("node_modules", StringComparison.OrdinalIgnoreCase) &&
                           !Path.GetFileName(d).Equals("bin", StringComparison.OrdinalIgnoreCase) &&
                           !Path.GetFileName(d).Equals("obj", StringComparison.OrdinalIgnoreCase));
            
            directories.AddRange(dirs);
            
            foreach (var dir in dirs)
            {
                directories.AddRange(await GetDirectoriesRecursive(dir, maxDepth, currentDepth + 1));
            }
        }
        catch
        {
            // Ignore inaccessible directories
        }
        
        return directories;
    }

    // Data models
    private class ArchitectureInfo
    {
        public string RootDirectory { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public List<Layer> Layers { get; set; } = new();
        public List<Component> Components { get; set; } = new();
        public List<Dependency>? Dependencies { get; set; }
        public List<string>? Patterns { get; set; }
        public Metrics? Metrics { get; set; }
    }

    private class Layer
    {
        public LayerType Type { get; set; }
        public List<string> Directories { get; set; } = new();
    }

    private enum LayerType
    {
        Domain,
        Application,
        Infrastructure,
        Presentation,
        Test,
        Unknown
    }

    private class Component
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public ComponentType Type { get; set; }
    }

    private enum ComponentType
    {
        Controller,
        Service,
        Repository,
        Model,
        ViewModel,
        Test,
        Other
    }

    private class Dependency
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    private class Metrics
    {
        public int TotalFiles { get; set; }
        public int TotalLinesOfCode { get; set; }
        public int LayerCount { get; set; }
        public int DependencyCount { get; set; }
        public Dictionary<string, int> ComponentCounts { get; set; } = new();
    }
}