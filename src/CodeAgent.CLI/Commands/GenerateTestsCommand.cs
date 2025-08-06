using System.ComponentModel;
using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class GenerateTestsCommand : AsyncCommand<GenerateTestsCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IChatService _chatService;

    public GenerateTestsCommand(IFileSystemService fileSystemService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("File or directory to generate tests for")]
        [CommandArgument(0, "<path>")]
        public string Path { get; set; } = string.Empty;

        [Description("Test framework to use (xunit, nunit, mstest, jest, mocha, pytest)")]
        [CommandOption("-f|--framework")]
        public string Framework { get; set; } = "xunit";

        [Description("Output directory for test files")]
        [CommandOption("-o|--output")]
        public string? OutputDirectory { get; set; }

        [Description("Include edge cases and error scenarios")]
        [CommandOption("--edge-cases")]
        public bool IncludeEdgeCases { get; set; } = true;

        [Description("Include integration tests")]
        [CommandOption("--integration")]
        public bool IncludeIntegrationTests { get; set; }

        [Description("Coverage target percentage")]
        [CommandOption("-c|--coverage")]
        public int CoverageTarget { get; set; } = 80;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold cyan]Generating Tests[/]"));
            
            // Determine if path is file or directory
            bool isFile = await _fileSystemService.FileExistsAsync(settings.Path);
            string[] files;
            
            if (isFile)
            {
                files = new[] { settings.Path };
            }
            else
            {
                // Get all code files in directory
                files = await GetCodeFiles(settings.Path);
                if (!files.Any())
                {
                    AnsiConsole.MarkupLine($"[yellow]No code files found in: {settings.Path}[/]");
                    return 0;
                }
            }
            
            AnsiConsole.MarkupLine($"[cyan]Generating tests for {files.Length} file(s) using {settings.Framework}[/]");
            
            // Determine output directory
            string outputDir = settings.OutputDirectory ?? GetDefaultTestDirectory(settings.Path);
            await _fileSystemService.CreateDirectoryAsync(outputDir);
            
            var generatedTests = new List<(string originalFile, string testFile, string content)>();
            
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Generating test files...[/]", maxValue: files.Length);
                    
                    foreach (var file in files)
                    {
                        task.Description = $"[green]Generating tests for: {Path.GetFileName(file)}[/]";
                        
                        var testContent = await GenerateTestsForFile(file, settings);
                        if (!string.IsNullOrWhiteSpace(testContent))
                        {
                            var testFileName = GenerateTestFileName(file, settings.Framework);
                            var testFilePath = Path.Combine(outputDir, testFileName);
                            
                            generatedTests.Add((file, testFilePath, testContent));
                        }
                        
                        task.Increment(1);
                    }
                });
            
            // Write test files
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync("Writing test files...", async ctx =>
                {
                    foreach (var (_, testFile, content) in generatedTests)
                    {
                        await _fileSystemService.WriteFileAsync(testFile, content);
                    }
                });
            
            // Display summary
            DisplayTestSummary(generatedTests, settings);
            
            // Optionally generate test configuration files
            if (generatedTests.Any())
            {
                await GenerateTestConfiguration(outputDir, settings.Framework);
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<string[]> GetCodeFiles(string directory)
    {
        var extensions = new[] { "*.cs", "*.ts", "*.js", "*.py", "*.java", "*.go", "*.rb" };
        var files = new List<string>();
        
        foreach (var ext in extensions)
        {
            var foundFiles = await _fileSystemService.GetFilesAsync(directory, ext, true);
            files.AddRange(foundFiles.Where(f => !f.Contains("test", StringComparison.OrdinalIgnoreCase)));
        }
        
        return files.ToArray();
    }

    private async Task<string> GenerateTestsForFile(string filePath, Settings settings)
    {
        var content = await _fileSystemService.ReadFileAsync(filePath);
        
        // Skip if file is too small or is already a test file
        if (content.Length < 50 || filePath.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }
        
        var prompt = new StringBuilder();
        prompt.AppendLine($"Generate comprehensive unit tests for the following code:");
        prompt.AppendLine($"File: {Path.GetFileName(filePath)}");
        prompt.AppendLine($"Test Framework: {settings.Framework}");
        prompt.AppendLine($"Coverage Target: {settings.CoverageTarget}%");
        prompt.AppendLine();
        prompt.AppendLine("Code to test:");
        prompt.AppendLine(content);
        prompt.AppendLine();
        prompt.AppendLine("Requirements:");
        prompt.AppendLine("1. Generate complete, runnable test code");
        prompt.AppendLine("2. Include appropriate imports and setup");
        prompt.AppendLine("3. Test all public methods and functions");
        prompt.AppendLine("4. Use descriptive test names");
        prompt.AppendLine("5. Include assertions for expected behavior");
        
        if (settings.IncludeEdgeCases)
        {
            prompt.AppendLine("6. Include edge cases and error scenarios");
            prompt.AppendLine("7. Test boundary conditions");
            prompt.AppendLine("8. Test null/empty/invalid inputs");
        }
        
        if (settings.IncludeIntegrationTests)
        {
            prompt.AppendLine("9. Include integration test scenarios");
            prompt.AppendLine("10. Test component interactions");
        }
        
        prompt.AppendLine();
        prompt.AppendLine($"Generate tests using {settings.Framework} syntax and best practices.");
        prompt.AppendLine();
        prompt.AppendLine("IMPORTANT: Return ONLY the test code. Do NOT include explanations, markdown formatting, or code blocks.");
        
        var result = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt.ToString()))
        {
            result.Append(chunk);
        }
        
        return result.ToString();
    }

    private string GenerateTestFileName(string originalFile, string framework)
    {
        var fileName = Path.GetFileNameWithoutExtension(originalFile);
        var extension = Path.GetExtension(originalFile);
        
        return framework.ToLower() switch
        {
            "xunit" or "nunit" or "mstest" => $"{fileName}Tests{extension}",
            "jest" or "mocha" => $"{fileName}.test{extension}",
            "pytest" => $"test_{fileName}.py",
            _ => $"{fileName}Test{extension}"
        };
    }

    private string GetDefaultTestDirectory(string sourcePath)
    {
        if (File.Exists(sourcePath))
        {
            sourcePath = Path.GetDirectoryName(sourcePath) ?? ".";
        }
        
        // Look for existing test directory patterns
        var parentDir = Directory.GetParent(sourcePath)?.FullName ?? sourcePath;
        var possibleTestDirs = new[] { "tests", "test", "Tests", "Test", "__tests__", "spec" };
        
        foreach (var testDir in possibleTestDirs)
        {
            var testPath = Path.Combine(parentDir, testDir);
            if (Directory.Exists(testPath))
            {
                return testPath;
            }
        }
        
        // Create a tests directory if none exists
        return Path.Combine(parentDir, "tests");
    }

    private void DisplayTestSummary(List<(string originalFile, string testFile, string content)> generatedTests, Settings settings)
    {
        var table = new Table();
        table.AddColumn("Source File");
        table.AddColumn("Test File");
        table.AddColumn("Test Count");
        table.Border(TableBorder.Rounded);
        
        foreach (var (original, test, content) in generatedTests)
        {
            var testCount = CountTests(content, settings.Framework);
            table.AddRow(
                Path.GetFileName(original),
                Path.GetFileName(test),
                testCount.ToString()
            );
        }
        
        AnsiConsole.Write(table);
        
        var panel = new Panel($"[green]✓ Generated {generatedTests.Count} test file(s)[/]\n" +
                            $"Framework: {settings.Framework}\n" +
                            $"Coverage Target: {settings.CoverageTarget}%")
        {
            Header = new PanelHeader(" Test Generation Summary "),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);
    }

    private int CountTests(string content, string framework)
    {
        return framework.ToLower() switch
        {
            "xunit" => content.Split("[Fact]").Length - 1 + content.Split("[Theory]").Length - 1,
            "nunit" => content.Split("[Test]").Length - 1,
            "mstest" => content.Split("[TestMethod]").Length - 1,
            "jest" or "mocha" => content.Split("it(").Length - 1 + content.Split("test(").Length - 1,
            "pytest" => content.Split("def test_").Length - 1,
            _ => 0
        };
    }

    private async Task GenerateTestConfiguration(string outputDir, string framework)
    {
        string configContent = framework.ToLower() switch
        {
            "jest" => @"{
  ""testEnvironment"": ""node"",
  ""coverageDirectory"": ""coverage"",
  ""collectCoverageFrom"": [
    ""src/**/*.{js,ts}"",
    ""!src/**/*.test.{js,ts}""
  ]
}",
            "mocha" => @"{
  ""require"": [""@babel/register""],
  ""recursive"": true,
  ""exit"": true
}",
            "pytest" => @"[tool.pytest.ini_options]
testpaths = [""tests""]
python_files = ""test_*.py""
python_classes = ""Test*""
python_functions = ""test_*""",
            _ => string.Empty
        };
        
        if (!string.IsNullOrEmpty(configContent))
        {
            var configFile = framework.ToLower() switch
            {
                "jest" => "jest.config.json",
                "mocha" => ".mocharc.json",
                "pytest" => "pytest.ini",
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(configFile))
            {
                var configPath = Path.Combine(outputDir, configFile);
                if (!await _fileSystemService.FileExistsAsync(configPath))
                {
                    await _fileSystemService.WriteFileAsync(configPath, configContent);
                    AnsiConsole.MarkupLine($"[green]Created test configuration: {configFile}[/]");
                }
            }
        }
    }
}