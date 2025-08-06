using System.Text.Json;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models.Security;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<AuditService> _logger;
    private readonly List<AuditEntry> _auditEntries = new();
    private readonly string _auditLogPath;

    public AuditService(
        IFileSystemService fileSystemService,
        ILogger<AuditService> logger)
    {
        _fileSystemService = fileSystemService;
        _logger = logger;
        _auditLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "CodeAgent", "audit.log");
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        _auditEntries.Add(entry);
        
        // Also persist to file
        var logLine = JsonSerializer.Serialize(entry);
        await AppendToAuditLogAsync(logLine, cancellationToken);
        
        _logger.LogInformation("Audit: {EventType} - {Description}", entry.EventType, entry.Description);
    }

    public Task LogSecurityEventAsync(SecurityEventType eventType, string userId, string details, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry
        {
            UserId = userId,
            EventType = AuditEventType.SecurityPolicyChange,
            EventCategory = "Security",
            EventName = eventType.ToString(),
            Description = details,
            Severity = eventType switch
            {
                SecurityEventType.LoginSuccess => AuditSeverity.Info,
                SecurityEventType.LoginFailure => AuditSeverity.Warning,
                SecurityEventType.PermissionDenied => AuditSeverity.Warning,
                SecurityEventType.PolicyViolation => AuditSeverity.Error,
                SecurityEventType.SuspiciousActivity => AuditSeverity.Critical,
                SecurityEventType.SecurityAlert => AuditSeverity.Critical,
                _ => AuditSeverity.Info
            }
        };

        return LogAsync(entry, cancellationToken);
    }

    public Task LogFileOperationAsync(FileOperation operation, string filePath, string userId, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry
        {
            UserId = userId,
            EventType = operation == FileOperation.Read ? AuditEventType.FileAccess : AuditEventType.FileModification,
            EventCategory = "FileSystem",
            EventName = $"File.{operation}",
            Description = $"{operation} operation on {filePath}",
            ResourceType = "File",
            ResourceId = filePath,
            Severity = operation switch
            {
                FileOperation.Delete => AuditSeverity.Warning,
                FileOperation.Execute => AuditSeverity.Warning,
                _ => AuditSeverity.Info
            }
        };

        return LogAsync(entry, cancellationToken);
    }

    public Task LogProviderAccessAsync(string providerName, string userId, string operation, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry
        {
            UserId = userId,
            EventType = AuditEventType.ProviderAccess,
            EventCategory = "Provider",
            EventName = $"Provider.{operation}",
            Description = $"{operation} on provider {providerName}",
            ResourceType = "Provider",
            ResourceId = providerName,
            Severity = AuditSeverity.Info
        };

        return LogAsync(entry, cancellationToken);
    }

    public Task<IEnumerable<AuditEntry>> GetAuditLogsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var logs = _auditEntries
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .OrderByDescending(e => e.Timestamp);

        return Task.FromResult(logs.AsEnumerable());
    }

    public Task<IEnumerable<AuditEntry>> GetUserAuditLogsAsync(string userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var logs = _auditEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Timestamp)
            .Take(limit);

        return Task.FromResult(logs.AsEnumerable());
    }

    public Task<IEnumerable<AuditEntry>> SearchAuditLogsAsync(string query, CancellationToken cancellationToken = default)
    {
        var logs = _auditEntries
            .Where(e => 
                e.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.EventName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.ResourceId.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Timestamp);

        return Task.FromResult(logs.AsEnumerable());
    }

    public async Task<AuditReport> GenerateAuditReportAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var events = await GetAuditLogsAsync(from, to, cancellationToken);
        var eventsList = events.ToList();

        var report = new AuditReport
        {
            PeriodStart = from,
            PeriodEnd = to,
            TotalEvents = eventsList.Count,
            EventsByType = eventsList.GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key, g => g.Count()),
            EventsBySeverity = eventsList.GroupBy(e => e.Severity)
                .ToDictionary(g => g.Key, g => g.Count()),
            CriticalEvents = eventsList.Where(e => e.Severity == AuditSeverity.Critical).ToList(),
            TopUsers = eventsList.GroupBy(e => e.UserId)
                .Select(g => new CodeAgent.Domain.Models.Security.UserActivity
                {
                    UserId = g.Key,
                    EventCount = g.Count(),
                    LastActivity = g.Max(e => e.Timestamp),
                    TopActions = g.GroupBy(e => e.EventName)
                        .OrderByDescending(a => a.Count())
                        .Take(5)
                        .Select(a => a.Key)
                        .ToList()
                })
                .OrderByDescending(u => u.EventCount)
                .Take(10)
                .ToList(),
            TopResources = eventsList.Where(e => !string.IsNullOrEmpty(e.ResourceId))
                .GroupBy(e => new { e.ResourceId, e.ResourceType })
                .Select(g => new ResourceAccess
                {
                    ResourceId = g.Key.ResourceId,
                    ResourceType = g.Key.ResourceType,
                    AccessCount = g.Count(),
                    TopUsers = g.GroupBy(e => e.UserId)
                        .OrderByDescending(u => u.Count())
                        .Take(5)
                        .Select(u => u.Key)
                        .ToList()
                })
                .OrderByDescending(r => r.AccessCount)
                .Take(10)
                .ToList()
        };

        report.Statistics["FailureRate"] = eventsList.Count(e => !e.Success) / (double)Math.Max(1, eventsList.Count);
        report.Statistics["AverageEventsPerHour"] = eventsList.Count / Math.Max(1, (to - from).TotalHours);

        return report;
    }

    public Task<ComplianceReport> GenerateComplianceReportAsync(ComplianceStandard standard, CancellationToken cancellationToken = default)
    {
        var report = new ComplianceReport
        {
            Standard = standard,
            PeriodStart = DateTime.UtcNow.AddDays(-30),
            PeriodEnd = DateTime.UtcNow,
            OverallStatus = ComplianceStatus.PartiallyCompliant
        };

        // Add compliance controls based on standard
        switch (standard)
        {
            case ComplianceStandard.SOC2:
                report.Controls = GetSOC2Controls();
                break;
            case ComplianceStandard.ISO27001:
                report.Controls = GetISO27001Controls();
                break;
            default:
                report.Controls = GetGenericControls();
                break;
        }

        // Check for violations
        report.Violations = CheckComplianceViolations(standard);
        
        // Calculate overall status
        var compliantControls = report.Controls.Count(c => c.Status == ComplianceStatus.Compliant);
        var totalControls = report.Controls.Count;
        
        if (compliantControls == totalControls)
            report.OverallStatus = ComplianceStatus.Compliant;
        else if (compliantControls == 0)
            report.OverallStatus = ComplianceStatus.NonCompliant;
        else
            report.OverallStatus = ComplianceStatus.PartiallyCompliant;

        report.Metrics["CompliancePercentage"] = (compliantControls / (double)totalControls) * 100;
        report.Metrics["CriticalViolations"] = report.Violations.Count(v => v.Severity == AuditSeverity.Critical);
        
        report.Summary = $"Compliance assessment for {standard} shows {report.OverallStatus} status with " +
                        $"{compliantControls}/{totalControls} controls meeting requirements.";

        return Task.FromResult(report);
    }

    private List<ComplianceControl> GetSOC2Controls()
    {
        return new List<ComplianceControl>
        {
            new()
            {
                Id = "CC1.1",
                Name = "Security Policies",
                Description = "Organization has defined security policies",
                Status = ComplianceStatus.Compliant,
                Evidence = new List<string> { "Security policies implemented in SecurityService" }
            },
            new()
            {
                Id = "CC2.1",
                Name = "Access Control",
                Description = "Logical access controls are in place",
                Status = ComplianceStatus.Compliant,
                Evidence = new List<string> { "RBAC system implemented" }
            },
            new()
            {
                Id = "CC4.1",
                Name = "Monitoring",
                Description = "Security events are monitored",
                Status = ComplianceStatus.Compliant,
                Evidence = new List<string> { "Audit logging system active" }
            },
            new()
            {
                Id = "CC6.1",
                Name = "Logical Access",
                Description = "Logical access to systems is restricted",
                Status = ComplianceStatus.PartiallyCompliant,
                Notes = "MFA implementation in progress"
            }
        };
    }

    private List<ComplianceControl> GetISO27001Controls()
    {
        return new List<ComplianceControl>
        {
            new()
            {
                Id = "A.9.1",
                Name = "Access Control Policy",
                Description = "Access control policy established",
                Status = ComplianceStatus.Compliant,
                Evidence = new List<string> { "Access control policies defined" }
            },
            new()
            {
                Id = "A.12.4",
                Name = "Logging and Monitoring",
                Description = "Event logging and monitoring",
                Status = ComplianceStatus.Compliant,
                Evidence = new List<string> { "Comprehensive audit logging" }
            },
            new()
            {
                Id = "A.18.1",
                Name = "Compliance",
                Description = "Compliance with legal requirements",
                Status = ComplianceStatus.PartiallyCompliant,
                Notes = "Additional compliance checks needed"
            }
        };
    }

    private List<ComplianceControl> GetGenericControls()
    {
        return new List<ComplianceControl>
        {
            new()
            {
                Id = "GC-1",
                Name = "Basic Security",
                Description = "Basic security controls",
                Status = ComplianceStatus.Compliant
            }
        };
    }

    private List<ComplianceViolation> CheckComplianceViolations(ComplianceStandard standard)
    {
        var violations = new List<ComplianceViolation>();
        
        // Check for common violations
        var recentEvents = _auditEntries.Where(e => e.Timestamp > DateTime.UtcNow.AddDays(-7)).ToList();
        
        // Check for authentication failures
        var authFailures = recentEvents.Count(e => e.EventName == SecurityEventType.LoginFailure.ToString());
        if (authFailures > 10)
        {
            violations.Add(new ComplianceViolation
            {
                ControlId = standard == ComplianceStandard.SOC2 ? "CC2.1" : "A.9.1",
                Description = $"Excessive authentication failures detected: {authFailures}",
                Severity = AuditSeverity.Warning,
                RemediationSteps = "Review authentication logs and implement account lockout policies"
            });
        }

        return violations;
    }

    private async Task AppendToAuditLogAsync(string logLine, CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(_auditLogPath);
            if (!string.IsNullOrEmpty(directory) && !await _fileSystemService.DirectoryExistsAsync(directory, cancellationToken))
            {
                await _fileSystemService.CreateDirectoryAsync(directory, cancellationToken);
            }

            var existingContent = await _fileSystemService.FileExistsAsync(_auditLogPath, cancellationToken)
                ? await _fileSystemService.ReadFileAsync(_auditLogPath, cancellationToken)
                : string.Empty;

            var newContent = string.IsNullOrEmpty(existingContent)
                ? logLine
                : existingContent + Environment.NewLine + logLine;

            await _fileSystemService.WriteFileAsync(_auditLogPath, newContent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log to file");
        }
    }
}