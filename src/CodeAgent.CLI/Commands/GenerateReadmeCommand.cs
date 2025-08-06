using System.ComponentModel;
using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class GenerateReadmeCommand : AsyncCommand<GenerateReadmeCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IChatService _chatService;
    private readonly IGitService _gitService;

    public GenerateReadmeCommand(IFileSystemService fileSystemService, IChatService chatService, IGitService gitService)
    {
        _fileSystemService = fileSystemService;
        _chatService = chatService;
        _gitService = gitService;
    }

    public class Settings : CommandSettings
    {
        [Description("Project directory")]
        [CommandArgument(0, "[directory]")]
        public string Directory { get; set; } = ".";

        [Description("Include installation instructions")]
        [CommandOption("--install")]
        public bool IncludeInstallation { get; set; } = true;

        [Description("Include usage examples")]
        [CommandOption("--examples")]
        public bool IncludeExamples { get; set; } = true;

        [Description("Include API documentation")]
        [CommandOption("--api")]
        public bool IncludeApi { get; set; } = true;

        [Description("Include contributing guidelines")]
        [CommandOption("--contributing")]
        public bool IncludeContributing { get; set; } = true;

        [Description("Include license information")]
        [CommandOption("--license")]
        public bool IncludeLicense { get; set; } = true;

        [Description("Output file path")]
        [CommandOption("-o|--output")]
        public string OutputPath { get; set; } = "README.md";

        [Description("Backup existing README")]
        [CommandOption("--backup")]
        public bool BackupExisting { get; set; } = true;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold cyan]Generate README[/]"));
            
            // Check if README already exists
            var readmePath = Path.Combine(settings.Directory, settings.OutputPath);
            if (await _fileSystemService.FileExistsAsync(readmePath) && settings.BackupExisting)
            {
                var backupPath = $"{readmePath}.backup.{DateTime.Now:yyyyMMddHHmmss}";
                var existingContent = await _fileSystemService.ReadFileAsync(readmePath);
                await _fileSystemService.WriteFileAsync(backupPath, existingContent);
                AnsiConsole.MarkupLine($"[yellow]Backed up existing README to: {Path.GetFileName(backupPath)}[/]");
            }
            
            // Gather project information
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync("Analyzing project structure...", async ctx =>
                {
                    var projectInfo = await GatherProjectInfo(settings);
                    
                    ctx.Status = "Generating README content...";
                    var readmeContent = await GenerateReadmeContent(projectInfo, settings);
                    
                    ctx.Status = "Writing README file...";
                    await _fileSystemService.WriteFileAsync(readmePath, readmeContent);
                });
            
            // Display preview
            var readmeText = await _fileSystemService.ReadFileAsync(readmePath);
            var previewText = readmeText.Length > 1000 ? readmeText.Substring(0, 1000) + "..." : readmeText;
            var panel = new Panel(new Text(previewText))
            {
                Header = new PanelHeader(" README Preview "),
                Border = BoxBorder.Rounded,
                Height = 20
            };
            AnsiConsole.Write(panel);
            
            AnsiConsole.MarkupLine($"[green]✓ README generated successfully at: {readmePath}[/]");
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<ProjectInfo> GatherProjectInfo(Settings settings)
    {
        var info = new ProjectInfo
        {
            Directory = settings.Directory,
            Name = Path.GetFileName(Path.GetFullPath(settings.Directory))
        };
        
        // Check for various project files
        info.HasPackageJson = await _fileSystemService.FileExistsAsync(Path.Combine(settings.Directory, "package.json"));
        info.HasCsproj = (await _fileSystemService.GetFilesAsync(settings.Directory, "*.csproj", true)).Any();
        info.HasSolution = (await _fileSystemService.GetFilesAsync(settings.Directory, "*.sln", true)).Any();
        info.HasPyProject = await _fileSystemService.FileExistsAsync(Path.Combine(settings.Directory, "pyproject.toml"));
        info.HasRequirementsTxt = await _fileSystemService.FileExistsAsync(Path.Combine(settings.Directory, "requirements.txt"));
        info.HasCargoToml = await _fileSystemService.FileExistsAsync(Path.Combine(settings.Directory, "Cargo.toml"));
        info.HasGoMod = await _fileSystemService.FileExistsAsync(Path.Combine(settings.Directory, "go.mod"));
        
        // Detect license
        var licenseFiles = new[] { "LICENSE", "LICENSE.txt", "LICENSE.md", "LICENCE", "LICENCE.txt", "LICENCE.md" };
        foreach (var licenseFile in licenseFiles)
        {
            var licensePath = Path.Combine(settings.Directory, licenseFile);
            if (await _fileSystemService.FileExistsAsync(licensePath))
            {
                info.LicenseFile = licenseFile;
                info.LicenseContent = await _fileSystemService.ReadFileAsync(licensePath);
                break;
            }
        }
        
        // Get Git info
        if (await _gitService.IsRepositoryAsync(settings.Directory))
        {
            info.IsGitRepository = true;
            info.GitRemoteUrl = await _gitService.GetRemoteUrlAsync(settings.Directory);
        }
        
        // Get source files for analysis
        var extensions = new[] { "*.cs", "*.ts", "*.js", "*.py", "*.java", "*.go", "*.rs", "*.cpp", "*.c" };
        var sourceFiles = new List<string>();
        foreach (var ext in extensions)
        {
            sourceFiles.AddRange(await _fileSystemService.GetFilesAsync(settings.Directory, ext, true));
        }
        info.SourceFiles = sourceFiles.ToArray();
        
        // Read existing documentation
        var docsDir = Path.Combine(settings.Directory, "docs");
        if (await _fileSystemService.DirectoryExistsAsync(docsDir))
        {
            info.HasDocsFolder = true;
            info.DocFiles = await _fileSystemService.GetFilesAsync(docsDir, "*.md", true);
        }
        
        return info;
    }

    private async Task<string> GenerateReadmeContent(ProjectInfo projectInfo, Settings settings)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Generate a professional README.md file for this project based on the following information:");
        prompt.AppendLine();
        prompt.AppendLine($"Project Name: {projectInfo.Name}");
        prompt.AppendLine($"Project Type: {DetectProjectType(projectInfo)}");
        
        if (projectInfo.IsGitRepository && !string.IsNullOrEmpty(projectInfo.GitRemoteUrl))
        {
            prompt.AppendLine($"Repository URL: {projectInfo.GitRemoteUrl}");
        }
        
        prompt.AppendLine($"Number of source files: {projectInfo.SourceFiles.Length}");
        
        // Include sample source files for context
        if (projectInfo.SourceFiles.Any())
        {
            prompt.AppendLine("\nSample source file structure:");
            var sampleFiles = projectInfo.SourceFiles.Take(10);
            foreach (var file in sampleFiles)
            {
                var relativePath = Path.GetRelativePath(projectInfo.Directory, file);
                prompt.AppendLine($"  - {relativePath}");
            }
            
            // Include a key file's content for better understanding
            var mainFile = FindMainFile(projectInfo);
            if (!string.IsNullOrEmpty(mainFile))
            {
                prompt.AppendLine($"\nMain file content ({Path.GetFileName(mainFile)}):");
                var content = await _fileSystemService.ReadFileAsync(mainFile);
                var truncatedContent = content.Length > 2000 ? content.Substring(0, 2000) + "..." : content;
                prompt.AppendLine(truncatedContent);
            }
        }
        
        prompt.AppendLine("\nGenerate a README with the following sections:");
        prompt.AppendLine("1. Project title and description");
        prompt.AppendLine("2. Features (bullet points of key features)");
        
        if (settings.IncludeInstallation)
        {
            prompt.AppendLine("3. Installation instructions (based on project type)");
            prompt.AppendLine("4. Prerequisites");
        }
        
        if (settings.IncludeExamples)
        {
            prompt.AppendLine("5. Usage examples with code snippets");
            prompt.AppendLine("6. Configuration options if applicable");
        }
        
        if (settings.IncludeApi && projectInfo.SourceFiles.Any())
        {
            prompt.AppendLine("7. API reference or key components overview");
        }
        
        if (settings.IncludeContributing)
        {
            prompt.AppendLine("8. Contributing guidelines");
            prompt.AppendLine("9. Development setup instructions");
        }
        
        if (settings.IncludeLicense && !string.IsNullOrEmpty(projectInfo.LicenseFile))
        {
            prompt.AppendLine($"10. License section (License type detected: {DetectLicenseType(projectInfo.LicenseContent)})");
        }
        
        prompt.AppendLine("\nAdditional requirements:");
        prompt.AppendLine("- Use shields.io badges where appropriate");
        prompt.AppendLine("- Include a table of contents for easy navigation");
        prompt.AppendLine("- Use proper Markdown formatting with headers, code blocks, and lists");
        prompt.AppendLine("- Make it professional and comprehensive");
        prompt.AppendLine("- Include contact or support information");
        
        var result = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt.ToString()))
        {
            result.Append(chunk);
        }
        
        return result.ToString();
    }

    private string DetectProjectType(ProjectInfo info)
    {
        if (info.HasSolution || info.HasCsproj) return ".NET/C#";
        if (info.HasPackageJson) return "Node.js/JavaScript/TypeScript";
        if (info.HasPyProject || info.HasRequirementsTxt) return "Python";
        if (info.HasCargoToml) return "Rust";
        if (info.HasGoMod) return "Go";
        return "General";
    }

    private string? FindMainFile(ProjectInfo info)
    {
        var mainPatterns = new[]
        {
            "Program.cs", "Main.cs", "main.py", "index.js", "index.ts",
            "app.js", "app.py", "main.go", "main.rs", "main.cpp", "main.c"
        };
        
        foreach (var pattern in mainPatterns)
        {
            var mainFile = info.SourceFiles.FirstOrDefault(f => 
                Path.GetFileName(f).Equals(pattern, StringComparison.OrdinalIgnoreCase));
            if (mainFile != null) return mainFile;
        }
        
        return info.SourceFiles.FirstOrDefault();
    }

    private string DetectLicenseType(string? licenseContent)
    {
        if (string.IsNullOrEmpty(licenseContent)) return "Unknown";
        
        var lower = licenseContent.ToLower();
        if (lower.Contains("mit license")) return "MIT";
        if (lower.Contains("apache license") && lower.Contains("version 2.0")) return "Apache 2.0";
        if (lower.Contains("gnu general public license")) 
        {
            if (lower.Contains("version 3")) return "GPL-3.0";
            if (lower.Contains("version 2")) return "GPL-2.0";
        }
        if (lower.Contains("bsd")) return "BSD";
        if (lower.Contains("mozilla public license")) return "MPL";
        if (lower.Contains("unlicense")) return "Unlicense";
        
        return "Custom";
    }

    private class ProjectInfo
    {
        public string Directory { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool HasPackageJson { get; set; }
        public bool HasCsproj { get; set; }
        public bool HasSolution { get; set; }
        public bool HasPyProject { get; set; }
        public bool HasRequirementsTxt { get; set; }
        public bool HasCargoToml { get; set; }
        public bool HasGoMod { get; set; }
        public bool IsGitRepository { get; set; }
        public string? GitRemoteUrl { get; set; }
        public string? LicenseFile { get; set; }
        public string? LicenseContent { get; set; }
        public bool HasDocsFolder { get; set; }
        public string[] DocFiles { get; set; } = Array.Empty<string>();
        public string[] SourceFiles { get; set; } = Array.Empty<string>();
    }
}