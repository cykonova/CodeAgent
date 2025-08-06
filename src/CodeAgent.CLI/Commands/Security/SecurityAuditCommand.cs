using System.ComponentModel;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models.Security;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeAgent.CLI.Commands.Security;

[Description("Perform comprehensive security audit")]
public class SecurityAuditCommand : AsyncCommand<SecurityAuditCommand.Settings>
{
    private readonly IAuditService _auditService;
    private readonly ISecurityService _securityService;
    private readonly IDlpService _dlpService;
    private readonly IFileSystemService _fileSystemService;

    public SecurityAuditCommand(
        IAuditService auditService,
        ISecurityService securityService,
        IDlpService dlpService,
        IFileSystemService fileSystemService)
    {
        _auditService = auditService;
        _securityService = securityService;
        _dlpService = dlpService;
        _fileSystemService = fileSystemService;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-d|--days")]
        [Description("Number of days to audit (default: 30)")]
        public int Days { get; set; } = 30;

        [CommandOption("-o|--output")]
        [Description("Output file for audit report")]
        public string? OutputFile { get; set; }

        [CommandOption("--deep")]
        [Description("Perform deep security analysis")]
        public bool DeepScan { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await AnsiConsole.Status()
            .StartAsync("Performing security audit...", async ctx =>
            {
                ctx.Status("Analyzing audit logs...");
                var auditReport = await _auditService.GenerateAuditReportAsync(
                    DateTime.UtcNow.AddDays(-settings.Days),
                    DateTime.UtcNow);

                ctx.Status("Checking security policies...");
                var policies = await _securityService.GetPoliciesAsync();
                
                ctx.Status("Reviewing access controls...");
                var roles = await _securityService.GetRolesAsync();

                if (settings.DeepScan)
                {
                    ctx.Status("Performing DLP scan...");
                    var currentDir = Directory.GetCurrentDirectory();
                    var dlpResult = await _dlpService.ScanDirectoryAsync(currentDir);
                    
                    if (dlpResult.HasSensitiveData)
                    {
                        AnsiConsole.MarkupLine("[yellow]Warning: Sensitive data detected![/]");
                        DisplayDlpFindings(dlpResult);
                    }
                }

                // Display audit report
                DisplayAuditReport(auditReport);
                
                // Display security status
                DisplaySecurityStatus(policies, roles);

                // Generate compliance reports
                ctx.Status("Generating compliance reports...");
                var soc2Report = await _auditService.GenerateComplianceReportAsync(ComplianceStandard.SOC2);
                DisplayComplianceStatus(soc2Report);

                if (!string.IsNullOrEmpty(settings.OutputFile))
                {
                    ctx.Status("Writing report to file...");
                    await WriteReportToFile(settings.OutputFile, auditReport, soc2Report);
                }
            });

        AnsiConsole.MarkupLine("[green]Security audit completed successfully![/]");
        return 0;
    }

    private void DisplayAuditReport(AuditReport report)
    {
        var panel = new Panel(
            new Rows(
                new Markup($"[bold]Audit Period:[/] {report.PeriodStart:yyyy-MM-dd} to {report.PeriodEnd:yyyy-MM-dd}"),
                new Markup($"[bold]Total Events:[/] {report.TotalEvents}"),
                new Markup($"[bold]Critical Events:[/] [red]{report.CriticalEvents.Count}[/]")
            ))
        {
            Header = new PanelHeader("Audit Summary"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);

        if (report.EventsByType.Any())
        {
            var chart = new BreakdownChart()
                .Width(60)
                .ShowPercentage();

            foreach (var (eventType, count) in report.EventsByType)
            {
                chart.AddItem(eventType.ToString(), count, Color.Blue);
            }

            AnsiConsole.Write(new Panel(chart) { Header = new PanelHeader("Events by Type") });
        }

        if (report.TopUsers.Any())
        {
            var table = new Table()
                .AddColumn("User")
                .AddColumn("Events")
                .AddColumn("Last Activity");

            foreach (var user in report.TopUsers.Take(5))
            {
                table.AddRow(
                    user.UserId,
                    user.EventCount.ToString(),
                    user.LastActivity.ToString("yyyy-MM-dd HH:mm"));
            }

            AnsiConsole.Write(new Panel(table) { Header = new PanelHeader("Top Users") });
        }
    }

    private void DisplaySecurityStatus(IEnumerable<SecurityPolicy> policies, IEnumerable<Role> roles)
    {
        var grid = new Grid()
            .AddColumn()
            .AddColumn()
            .AddRow(
                new Panel($"[bold]Active Policies:[/] {policies.Count()}")
                    .Header("Security Policies"),
                new Panel($"[bold]Defined Roles:[/] {roles.Count()}")
                    .Header("Access Control"));

        AnsiConsole.Write(grid);
    }

    private void DisplayComplianceStatus(ComplianceReport report)
    {
        var statusColor = report.OverallStatus switch
        {
            ComplianceStatus.Compliant => "green",
            ComplianceStatus.PartiallyCompliant => "yellow",
            ComplianceStatus.NonCompliant => "red",
            _ => "grey"
        };

        AnsiConsole.MarkupLine($"\n[bold]Compliance Status ({report.Standard}):[/] [{statusColor}]{report.OverallStatus}[/]");

        if (report.Violations.Any())
        {
            AnsiConsole.MarkupLine($"[red]Found {report.Violations.Count} compliance violations[/]");
            
            var table = new Table()
                .AddColumn("Control")
                .AddColumn("Description")
                .AddColumn("Severity");

            foreach (var violation in report.Violations.Take(5))
            {
                table.AddRow(
                    violation.ControlId,
                    Markup.Escape(violation.Description),
                    violation.Severity.ToString());
            }

            AnsiConsole.Write(table);
        }
    }

    private void DisplayDlpFindings(DlpScanResult result)
    {
        var table = new Table()
            .AddColumn("Type")
            .AddColumn("Count")
            .AddColumn("Sensitivity");

        foreach (var (type, count) in result.FindingCounts)
        {
            var sensitivity = result.Findings
                .Where(f => f.Type == type)
                .Max(f => f.Sensitivity);
                
            var color = sensitivity switch
            {
                SensitivityLevel.Critical => "red",
                SensitivityLevel.High => "orange1",
                SensitivityLevel.Medium => "yellow",
                _ => "white"
            };

            table.AddRow(
                type,
                count.ToString(),
                $"[{color}]{sensitivity}[/]");
        }

        AnsiConsole.Write(new Panel(table) { Header = new PanelHeader("DLP Findings") });
    }

    private async Task WriteReportToFile(string filePath, AuditReport auditReport, ComplianceReport complianceReport)
    {
        var report = $"""
            Security Audit Report
            =====================
            Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            
            Audit Summary
            -------------
            Period: {auditReport.PeriodStart:yyyy-MM-dd} to {auditReport.PeriodEnd:yyyy-MM-dd}
            Total Events: {auditReport.TotalEvents}
            Critical Events: {auditReport.CriticalEvents.Count}
            
            Compliance Status
            -----------------
            Standard: {complianceReport.Standard}
            Status: {complianceReport.OverallStatus}
            Violations: {complianceReport.Violations.Count}
            
            {complianceReport.Summary}
            """;

        await _fileSystemService.WriteFileAsync(filePath, report);
    }
}