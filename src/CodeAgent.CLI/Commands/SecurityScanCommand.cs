using System.ComponentModel;
using System.Text;
using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands;

public class SecurityScanCommand : AsyncCommand<SecurityScanCommand.Settings>
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IChatService _chatService;

    public SecurityScanCommand(IFileSystemService fileSystemService, IChatService chatService)
    {
        _fileSystemService = fileSystemService;
        _chatService = chatService;
    }

    public class Settings : CommandSettings
    {
        [Description("File or directory to scan")]
        [CommandArgument(0, "<path>")]
        public string Path { get; set; } = string.Empty;

        [Description("Scan depth (basic, standard, deep)")]
        [CommandOption("-d|--depth")]
        public ScanDepth Depth { get; set; } = ScanDepth.Standard;

        [Description("Include dependency scanning")]
        [CommandOption("--dependencies")]
        public bool ScanDependencies { get; set; } = true;

        [Description("Include secret detection")]
        [CommandOption("--secrets")]
        public bool ScanSecrets { get; set; } = true;

        [Description("Include OWASP checks")]
        [CommandOption("--owasp")]
        public bool IncludeOwasp { get; set; } = true;

        [Description("Output format (console, json, sarif, markdown)")]
        [CommandOption("-f|--format")]
        public OutputFormat Format { get; set; } = OutputFormat.Console;

        [Description("Output file for results")]
        [CommandOption("-o|--output")]
        public string? OutputFile { get; set; }

        [Description("Fail on high severity issues")]
        [CommandOption("--fail-on-high")]
        public bool FailOnHigh { get; set; } = true;

        [Description("Include fix suggestions")]
        [CommandOption("--suggest-fixes")]
        public bool SuggestFixes { get; set; } = true;
    }

    public enum ScanDepth
    {
        Basic,
        Standard,
        Deep
    }

    public enum OutputFormat
    {
        Console,
        Json,
        Sarif,
        Markdown
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold red]Security Scan[/]"));
            
            // Determine if path is file or directory
            bool isFile = await _fileSystemService.FileExistsAsync(settings.Path);
            bool isDirectory = await _fileSystemService.DirectoryExistsAsync(settings.Path);
            
            if (!isFile && !isDirectory)
            {
                AnsiConsole.MarkupLine($"[red]Error: Path '{settings.Path}' does not exist[/]");
                return 1;
            }
            
            // Get files to scan
            var files = isFile 
                ? new[] { settings.Path } 
                : await GetFilesToScan(settings.Path);
            
            if (!files.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No files found to scan[/]");
                return 0;
            }
            
            AnsiConsole.MarkupLine($"[cyan]Scanning {files.Length} file(s) - Depth: {settings.Depth}[/]");
            
            var vulnerabilities = new List<SecurityVulnerability>();
            
            // Scan each file
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[yellow]Scanning for vulnerabilities...[/]", maxValue: files.Length);
                    
                    foreach (var file in files)
                    {
                        task.Description = $"[yellow]Scanning: {Path.GetFileName(file)}[/]";
                        
                        var fileVulns = await ScanFile(file, settings);
                        vulnerabilities.AddRange(fileVulns);
                        
                        task.Increment(1);
                    }
                });
            
            // Scan dependencies if requested
            if (settings.ScanDependencies)
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .StartAsync("Scanning dependencies...", async ctx =>
                    {
                        var depVulns = await ScanDependencies(settings.Path);
                        vulnerabilities.AddRange(depVulns);
                    });
            }
            
            // Generate output
            string output = settings.Format switch
            {
                OutputFormat.Console => string.Empty, // Handled separately
                OutputFormat.Json => GenerateJsonOutput(vulnerabilities),
                OutputFormat.Sarif => GenerateSarifOutput(vulnerabilities, settings),
                OutputFormat.Markdown => GenerateMarkdownOutput(vulnerabilities, settings),
                _ => string.Empty
            };
            
            // Display or save output
            if (settings.Format == OutputFormat.Console)
            {
                DisplayConsoleOutput(vulnerabilities, settings);
            }
            else if (!string.IsNullOrEmpty(settings.OutputFile))
            {
                await _fileSystemService.WriteFileAsync(settings.OutputFile, output);
                AnsiConsole.MarkupLine($"[green]✓ Results saved to: {settings.OutputFile}[/]");
            }
            else
            {
                AnsiConsole.WriteLine(output);
            }
            
            // Display summary
            DisplaySummary(vulnerabilities);
            
            // Determine exit code
            bool hasHighSeverity = vulnerabilities.Any(v => v.Severity == "high" || v.Severity == "critical");
            return (settings.FailOnHigh && hasHighSeverity) ? 1 : 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<string[]> GetFilesToScan(string directory)
    {
        var extensions = new[] { 
            "*.cs", "*.ts", "*.js", "*.jsx", "*.tsx", "*.py", "*.java", 
            "*.go", "*.rs", "*.php", "*.rb", "*.yml", "*.yaml", "*.json",
            "*.xml", "*.config", "*.env", "*.properties", "*.sql"
        };
        var files = new List<string>();
        
        foreach (var ext in extensions)
        {
            var foundFiles = await _fileSystemService.GetFilesAsync(directory, ext, true);
            files.AddRange(foundFiles.Where(f => 
                !f.Contains("node_modules") && 
                !f.Contains("vendor") &&
                !f.Contains(".git")));
        }
        
        return files.ToArray();
    }

    private async Task<List<SecurityVulnerability>> ScanFile(string filePath, Settings settings)
    {
        var vulnerabilities = new List<SecurityVulnerability>();
        var content = await _fileSystemService.ReadFileAsync(filePath);
        
        if (string.IsNullOrWhiteSpace(content))
        {
            return vulnerabilities;
        }
        
        var language = DetectLanguage(filePath);
        var prompt = BuildSecurityScanPrompt(content, filePath, language, settings);
        
        var response = new StringBuilder();
        await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
        {
            response.Append(chunk);
        }
        
        return ParseSecurityResponse(response.ToString(), filePath);
    }

    private string BuildSecurityScanPrompt(string content, string filePath, string language, Settings settings)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Perform a security scan on the following {language} code:");
        prompt.AppendLine($"File: {Path.GetFileName(filePath)}");
        prompt.AppendLine($"Scan Depth: {settings.Depth}");
        prompt.AppendLine();
        prompt.AppendLine("Code:");
        prompt.AppendLine(content);
        prompt.AppendLine();
        prompt.AppendLine("Scan for the following security issues:");
        
        // Basic checks
        prompt.AppendLine("1. Hard-coded credentials and API keys");
        prompt.AppendLine("2. SQL injection vulnerabilities");
        prompt.AppendLine("3. Cross-site scripting (XSS) vulnerabilities");
        prompt.AppendLine("4. Insecure deserialization");
        prompt.AppendLine("5. Path traversal vulnerabilities");
        
        if (settings.Depth >= ScanDepth.Standard)
        {
            prompt.AppendLine("6. Command injection vulnerabilities");
            prompt.AppendLine("7. LDAP injection");
            prompt.AppendLine("8. XML external entity (XXE) vulnerabilities");
            prompt.AppendLine("9. Server-side request forgery (SSRF)");
            prompt.AppendLine("10. Insecure random number generation");
            prompt.AppendLine("11. Weak cryptography");
            prompt.AppendLine("12. Missing authentication/authorization checks");
            prompt.AppendLine("13. Sensitive data exposure");
            prompt.AppendLine("14. Insecure file operations");
            prompt.AppendLine("15. Race conditions");
        }
        
        if (settings.Depth == ScanDepth.Deep)
        {
            prompt.AppendLine("16. Buffer overflow vulnerabilities");
            prompt.AppendLine("17. Format string vulnerabilities");
            prompt.AppendLine("18. Integer overflow/underflow");
            prompt.AppendLine("19. Use after free vulnerabilities");
            prompt.AppendLine("20. Double free vulnerabilities");
            prompt.AppendLine("21. Memory leaks that could lead to DoS");
            prompt.AppendLine("22. Timing attacks");
            prompt.AppendLine("23. Side-channel vulnerabilities");
        }
        
        if (settings.ScanSecrets)
        {
            prompt.AppendLine("24. AWS credentials");
            prompt.AppendLine("25. Azure credentials");
            prompt.AppendLine("26. GCP credentials");
            prompt.AppendLine("27. Private keys");
            prompt.AppendLine("28. OAuth tokens");
            prompt.AppendLine("29. JWT secrets");
            prompt.AppendLine("30. Database connection strings");
        }
        
        if (settings.IncludeOwasp)
        {
            prompt.AppendLine("31. OWASP Top 10 vulnerabilities");
            prompt.AppendLine("32. CWE Top 25 vulnerabilities");
        }
        
        prompt.AppendLine();
        prompt.AppendLine("Return a JSON array with this structure:");
        prompt.AppendLine("[{");
        prompt.AppendLine("  \"type\": \"vulnerability type\",");
        prompt.AppendLine("  \"severity\": \"critical|high|medium|low|info\",");
        prompt.AppendLine("  \"title\": \"vulnerability title\",");
        prompt.AppendLine("  \"description\": \"detailed description\",");
        prompt.AppendLine("  \"location\": \"line number or range\",");
        prompt.AppendLine("  \"cwe\": \"CWE ID if applicable\",");
        prompt.AppendLine("  \"owasp\": \"OWASP category if applicable\",");
        prompt.AppendLine("  \"codeSnippet\": \"affected code\",");
        
        if (settings.SuggestFixes)
        {
            prompt.AppendLine("  \"remediation\": \"how to fix\",");
            prompt.AppendLine("  \"fixedCode\": \"corrected code example\"");
        }
        
        prompt.AppendLine("}]");
        
        return prompt.ToString();
    }

    private List<SecurityVulnerability> ParseSecurityResponse(string response, string filePath)
    {
        var vulnerabilities = new List<SecurityVulnerability>();
        
        try
        {
            // Find JSON in response
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']') + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    vulnerabilities.Add(new SecurityVulnerability
                    {
                        FilePath = filePath,
                        Type = element.GetProperty("type").GetString() ?? "Unknown",
                        Severity = element.GetProperty("severity").GetString() ?? "info",
                        Title = element.GetProperty("title").GetString() ?? "Security Issue",
                        Description = element.GetProperty("description").GetString() ?? "",
                        Location = element.TryGetProperty("location", out var loc) ? loc.GetString() ?? "" : "",
                        CWE = element.TryGetProperty("cwe", out var cwe) ? cwe.GetString() : null,
                        OWASP = element.TryGetProperty("owasp", out var owasp) ? owasp.GetString() : null,
                        CodeSnippet = element.TryGetProperty("codeSnippet", out var snippet) ? snippet.GetString() : null,
                        Remediation = element.TryGetProperty("remediation", out var rem) ? rem.GetString() : null,
                        FixedCode = element.TryGetProperty("fixedCode", out var fix) ? fix.GetString() : null
                    });
                }
            }
        }
        catch
        {
            // If parsing fails, create a generic vulnerability
            if (response.ToLower().Contains("vulnerability") || response.ToLower().Contains("security"))
            {
                vulnerabilities.Add(new SecurityVulnerability
                {
                    FilePath = filePath,
                    Type = "General",
                    Severity = "medium",
                    Title = "Security concerns detected",
                    Description = response
                });
            }
        }
        
        return vulnerabilities;
    }

    private async Task<List<SecurityVulnerability>> ScanDependencies(string path)
    {
        var vulnerabilities = new List<SecurityVulnerability>();
        
        // Check for package files
        var packageFiles = new[]
        {
            "package.json",
            "package-lock.json",
            "yarn.lock",
            "requirements.txt",
            "Pipfile.lock",
            "Gemfile.lock",
            "pom.xml",
            "build.gradle",
            "*.csproj",
            "go.mod",
            "Cargo.toml"
        };
        
        foreach (var pattern in packageFiles)
        {
            var files = await _fileSystemService.GetFilesAsync(path, pattern, true);
            
            foreach (var file in files)
            {
                var content = await _fileSystemService.ReadFileAsync(file);
                var prompt = $"Scan the following dependency file for known vulnerabilities:\n" +
                           $"File: {Path.GetFileName(file)}\n" +
                           $"Content:\n{content}\n\n" +
                           "Check for:\n" +
                           "1. Outdated packages with known vulnerabilities\n" +
                           "2. Packages with security advisories\n" +
                           "3. Deprecated or unmaintained packages\n" +
                           "4. License compliance issues\n\n" +
                           "Return vulnerabilities in JSON format as before.";
                
                var response = new StringBuilder();
                await foreach (var chunk in _chatService.StreamResponseAsync(prompt))
                {
                    response.Append(chunk);
                }
                
                var depVulns = ParseSecurityResponse(response.ToString(), file);
                vulnerabilities.AddRange(depVulns);
            }
        }
        
        return vulnerabilities;
    }

    private void DisplayConsoleOutput(List<SecurityVulnerability> vulnerabilities, Settings settings)
    {
        if (!vulnerabilities.Any())
        {
            AnsiConsole.MarkupLine("[green]✓ No security vulnerabilities detected![/]");
            return;
        }
        
        // Group by severity
        var bySeverity = vulnerabilities.GroupBy(v => v.Severity)
            .OrderBy(g => GetSeverityOrder(g.Key));
        
        foreach (var group in bySeverity)
        {
            var color = GetSeverityColor(group.Key);
            AnsiConsole.Write(new Rule($"[{color}]{group.Key.ToUpper()} SEVERITY[/]"));
            
            foreach (var vuln in group)
            {
                var panel = new Panel(new Rows(
                    new Markup($"[bold]{vuln.Title}[/]"),
                    new Markup($"Type: {vuln.Type}"),
                    new Markup($"File: {Path.GetRelativePath(".", vuln.FilePath)}"),
                    vuln.Location != null ? new Markup($"Location: {vuln.Location}") : Text.Empty,
                    new Markup($"\n{vuln.Description}"),
                    vuln.CWE != null ? new Markup($"\nCWE: {vuln.CWE}") : Text.Empty,
                    vuln.OWASP != null ? new Markup($"OWASP: {vuln.OWASP}") : Text.Empty,
                    vuln.Remediation != null ? new Markup($"\n[green]Fix:[/] {vuln.Remediation}") : Text.Empty
                ))
                {
                    Header = new PanelHeader($" [{color}]●[/] {vuln.Type} "),
                    Border = BoxBorder.Rounded
                };
                
                AnsiConsole.Write(panel);
            }
        }
    }

    private void DisplaySummary(List<SecurityVulnerability> vulnerabilities)
    {
        var table = new Table();
        table.AddColumn("Severity");
        table.AddColumn("Count");
        table.Border(TableBorder.Rounded);
        table.Title = new TableTitle("Security Scan Summary");
        
        var severities = new[] { "critical", "high", "medium", "low", "info" };
        foreach (var severity in severities)
        {
            var count = vulnerabilities.Count(v => v.Severity == severity);
            if (count > 0)
            {
                var color = GetSeverityColor(severity);
                table.AddRow($"[{color}]{severity.ToUpper()}[/]", count.ToString());
            }
        }
        
        table.AddRow("[bold]TOTAL[/]", vulnerabilities.Count.ToString());
        
        AnsiConsole.Write(table);
        
        if (vulnerabilities.Any(v => v.Severity == "critical" || v.Severity == "high"))
        {
            AnsiConsole.MarkupLine("\n[red]⚠ High severity vulnerabilities detected![/]");
        }
        else if (vulnerabilities.Any())
        {
            AnsiConsole.MarkupLine("\n[yellow]⚠ Security issues found - review recommended[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("\n[green]✓ No security issues detected[/]");
        }
    }

    private string GenerateJsonOutput(List<SecurityVulnerability> vulnerabilities)
    {
        return System.Text.Json.JsonSerializer.Serialize(vulnerabilities, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private string GenerateSarifOutput(List<SecurityVulnerability> vulnerabilities, Settings settings)
    {
        // SARIF 2.1.0 format
        var sarif = new
        {
            version = "2.1.0",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "CodeAgent Security Scanner",
                            version = "1.0.0",
                            informationUri = "https://github.com/codeagent"
                        }
                    },
                    results = vulnerabilities.Select(v => new
                    {
                        ruleId = v.CWE ?? v.Type,
                        level = MapSeverityToSarifLevel(v.Severity),
                        message = new { text = v.Description },
                        locations = new[]
                        {
                            new
                            {
                                physicalLocation = new
                                {
                                    artifactLocation = new { uri = v.FilePath },
                                    region = new { startLine = ParseLineNumber(v.Location) }
                                }
                            }
                        }
                    })
                }
            }
        };
        
        return System.Text.Json.JsonSerializer.Serialize(sarif, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private string GenerateMarkdownOutput(List<SecurityVulnerability> vulnerabilities, Settings settings)
    {
        var md = new StringBuilder();
        md.AppendLine("# Security Scan Report");
        md.AppendLine();
        md.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.AppendLine($"**Files Scanned:** {vulnerabilities.Select(v => v.FilePath).Distinct().Count()}");
        md.AppendLine($"**Total Issues:** {vulnerabilities.Count}");
        md.AppendLine();
        
        md.AppendLine("## Summary by Severity");
        var bySeverity = vulnerabilities.GroupBy(v => v.Severity);
        foreach (var group in bySeverity.OrderBy(g => GetSeverityOrder(g.Key)))
        {
            md.AppendLine($"- **{group.Key.ToUpper()}:** {group.Count()}");
        }
        md.AppendLine();
        
        md.AppendLine("## Vulnerabilities");
        foreach (var vuln in vulnerabilities.OrderBy(v => GetSeverityOrder(v.Severity)))
        {
            md.AppendLine($"### {vuln.Title}");
            md.AppendLine($"- **Severity:** {vuln.Severity.ToUpper()}");
            md.AppendLine($"- **Type:** {vuln.Type}");
            md.AppendLine($"- **File:** {vuln.FilePath}");
            if (!string.IsNullOrEmpty(vuln.Location))
                md.AppendLine($"- **Location:** {vuln.Location}");
            if (!string.IsNullOrEmpty(vuln.CWE))
                md.AppendLine($"- **CWE:** {vuln.CWE}");
            if (!string.IsNullOrEmpty(vuln.OWASP))
                md.AppendLine($"- **OWASP:** {vuln.OWASP}");
            
            md.AppendLine();
            md.AppendLine($"**Description:** {vuln.Description}");
            
            if (!string.IsNullOrEmpty(vuln.CodeSnippet))
            {
                md.AppendLine();
                md.AppendLine("**Affected Code:**");
                md.AppendLine("```");
                md.AppendLine(vuln.CodeSnippet);
                md.AppendLine("```");
            }
            
            if (!string.IsNullOrEmpty(vuln.Remediation))
            {
                md.AppendLine();
                md.AppendLine($"**Remediation:** {vuln.Remediation}");
                
                if (!string.IsNullOrEmpty(vuln.FixedCode))
                {
                    md.AppendLine();
                    md.AppendLine("**Fixed Code:**");
                    md.AppendLine("```");
                    md.AppendLine(vuln.FixedCode);
                    md.AppendLine("```");
                }
            }
            
            md.AppendLine();
        }
        
        return md.ToString();
    }

    private string DetectLanguage(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".cs" => "C#",
            ".ts" or ".tsx" => "TypeScript",
            ".js" or ".jsx" => "JavaScript",
            ".py" => "Python",
            ".java" => "Java",
            ".go" => "Go",
            ".rs" => "Rust",
            ".php" => "PHP",
            ".rb" => "Ruby",
            ".cpp" or ".cc" or ".c" => "C/C++",
            ".yml" or ".yaml" => "YAML",
            ".json" => "JSON",
            ".xml" or ".config" => "XML",
            ".sql" => "SQL",
            _ => "Unknown"
        };
    }

    private int GetSeverityOrder(string severity)
    {
        return severity?.ToLower() switch
        {
            "critical" => 0,
            "high" => 1,
            "medium" => 2,
            "low" => 3,
            "info" => 4,
            _ => 5
        };
    }

    private string GetSeverityColor(string severity)
    {
        return severity?.ToLower() switch
        {
            "critical" => "red",
            "high" => "orangered",
            "medium" => "orange",
            "low" => "yellow",
            "info" => "blue",
            _ => "grey"
        };
    }

    private string MapSeverityToSarifLevel(string severity)
    {
        return severity?.ToLower() switch
        {
            "critical" or "high" => "error",
            "medium" => "warning",
            "low" or "info" => "note",
            _ => "none"
        };
    }

    private int ParseLineNumber(string? location)
    {
        if (string.IsNullOrEmpty(location))
            return 1;
        
        // Try to extract line number from location string
        var numbers = System.Text.RegularExpressions.Regex.Matches(location, @"\d+");
        if (numbers.Count > 0 && int.TryParse(numbers[0].Value, out var line))
        {
            return line;
        }
        
        return 1;
    }

    private class SecurityVulnerability
    {
        public string FilePath { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? CWE { get; set; }
        public string? OWASP { get; set; }
        public string? CodeSnippet { get; set; }
        public string? Remediation { get; set; }
        public string? FixedCode { get; set; }
    }
}