using CodeAgent.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CodeAgent.CLI.Commands;

[Description("Display performance statistics and metrics")]
public class StatsCommand : AsyncCommand<StatsCommand.Settings>
{
    private readonly IPerformanceMonitor _performanceMonitor;

    public class Settings : CommandSettings
    {
        [CommandOption("-e|--export")]
        [Description("Export statistics to a file")]
        public string? ExportPath { get; set; }

        [CommandOption("-r|--reset")]
        [Description("Reset all metrics after displaying")]
        public bool Reset { get; set; }

        [CommandOption("-d|--detailed")]
        [Description("Show detailed statistics")]
        public bool Detailed { get; set; }
    }

    public StatsCommand(IPerformanceMonitor performanceMonitor)
    {
        _performanceMonitor = performanceMonitor;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var stats = _performanceMonitor.GetStatistics();

        // Display overview
        DisplayOverview(stats);

        // Display operation statistics
        if (stats.Operations.Any())
        {
            DisplayOperationStatistics(stats.Operations, settings.Detailed);
        }

        // Display metrics
        if (stats.Metrics.Any())
        {
            DisplayMetricStatistics(stats.Metrics, settings.Detailed);
        }

        // Display events
        if (stats.EventCounts.Any())
        {
            DisplayEventStatistics(stats.EventCounts);
        }

        // Export if requested
        if (!string.IsNullOrEmpty(settings.ExportPath))
        {
            await _performanceMonitor.ExportMetricsAsync(settings.ExportPath);
            AnsiConsole.MarkupLine($"[green]✓[/] Statistics exported to [cyan]{settings.ExportPath}[/]");
        }

        // Reset if requested
        if (settings.Reset)
        {
            _performanceMonitor.Reset();
            AnsiConsole.MarkupLine("[yellow]Metrics have been reset[/]");
        }

        return 0;
    }

    private void DisplayOverview(PerformanceStatistics stats)
    {
        var panel = new Panel($"""
            Runtime: {stats.TotalRuntime:hh\:mm\:ss}
            Started: {stats.StartTime:yyyy-MM-dd HH:mm:ss}
            Memory: {stats.TotalMemoryUsed / (1024 * 1024):F1} MB
            CPU Usage: {stats.CpuUsagePercent:F1}%
            Operations: {stats.Operations.Count}
            Metrics: {stats.Metrics.Count}
            Events: {stats.EventCounts.Values.Sum()}
            """)
            .Header("[cyan]Performance Overview[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Aqua);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private void DisplayOperationStatistics(Dictionary<string, OperationStatistics> operations, bool detailed)
    {
        var table = new Table()
            .Title("[yellow]Operation Statistics[/]")
            .Border(TableBorder.Rounded)
            .AddColumn("Operation")
            .AddColumn("Count")
            .AddColumn("Total (ms)")
            .AddColumn("Avg (ms)")
            .AddColumn("Min (ms)")
            .AddColumn("Max (ms)");

        if (detailed)
        {
            table.AddColumn("Std Dev");
        }

        foreach (var op in operations.Values.OrderByDescending(o => o.TotalDuration))
        {
            var row = new[]
            {
                op.Name,
                op.Count.ToString(),
                op.TotalDuration.ToString("F1"),
                op.AverageDuration.ToString("F1"),
                op.MinDuration.ToString("F1"),
                op.MaxDuration.ToString("F1")
            };

            if (detailed)
            {
                table.AddRow(row.Append(op.StandardDeviation.ToString("F1")).ToArray());
            }
            else
            {
                table.AddRow(row);
            }
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private void DisplayMetricStatistics(Dictionary<string, MetricStatistics> metrics, bool detailed)
    {
        var table = new Table()
            .Title("[yellow]Metric Statistics[/]")
            .Border(TableBorder.Rounded)
            .AddColumn("Metric")
            .AddColumn("Count")
            .AddColumn("Average")
            .AddColumn("Min")
            .AddColumn("Max");

        if (detailed)
        {
            table.AddColumn("Total");
        }

        foreach (var metric in metrics.Values.OrderBy(m => m.Name))
        {
            var row = new[]
            {
                metric.Name,
                metric.Count.ToString(),
                FormatValue(metric.Average),
                FormatValue(metric.Min),
                FormatValue(metric.Max)
            };

            if (detailed)
            {
                table.AddRow(row.Append(FormatValue(metric.Total)).ToArray());
            }
            else
            {
                table.AddRow(row);
            }
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private void DisplayEventStatistics(Dictionary<string, int> eventCounts)
    {
        var chart = new BarChart()
            .Width(60)
            .Label("[yellow]Event Counts[/]")
            .CenterLabel();

        foreach (var (eventName, count) in eventCounts.OrderByDescending(e => e.Value).Take(10))
        {
            chart.AddItem(eventName, count, Color.Aqua);
        }

        AnsiConsole.Write(chart);
        AnsiConsole.WriteLine();
    }

    private string FormatValue(double value)
    {
        if (Math.Abs(value) >= 1000000)
            return $"{value / 1000000:F1}M";
        if (Math.Abs(value) >= 1000)
            return $"{value / 1000:F1}K";
        return value.ToString("F1");
    }
}