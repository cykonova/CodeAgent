using System.Text.RegularExpressions;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models.Security;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Infrastructure.Services;

public class DlpService : IDlpService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IAuditService _auditService;
    private readonly ILogger<DlpService> _logger;
    private readonly Dictionary<string, DlpPolicy> _policies = new();
    private readonly List<DlpIncident> _incidents = new();

    public DlpService(
        IFileSystemService fileSystemService,
        IAuditService auditService,
        ILogger<DlpService> logger)
    {
        _fileSystemService = fileSystemService;
        _auditService = auditService;
        _logger = logger;
        InitializeDefaultPolicies();
    }

    private void InitializeDefaultPolicies()
    {
        // Create default DLP policies
        var piiPolicy = new DlpPolicy
        {
            Id = "pii-protection",
            Name = "PII Protection Policy",
            Description = "Protects personally identifiable information",
            Action = PolicyAction.Redact,
            Rules = new List<DlpRule>
            {
                new() { Name = "SSN", Type = DlpRuleType.Regex, Pattern = SensitiveDataPatterns.SSN, Sensitivity = SensitivityLevel.Critical },
                new() { Name = "Credit Card", Type = DlpRuleType.Regex, Pattern = SensitiveDataPatterns.CreditCard, Sensitivity = SensitivityLevel.Critical },
                new() { Name = "Email", Type = DlpRuleType.Regex, Pattern = SensitiveDataPatterns.Email, Sensitivity = SensitivityLevel.Medium },
                new() { Name = "Phone", Type = DlpRuleType.Regex, Pattern = SensitiveDataPatterns.Phone, Sensitivity = SensitivityLevel.Medium }
            }
        };
        _policies[piiPolicy.Id] = piiPolicy;

        var secretsPolicy = new DlpPolicy
        {
            Id = "secrets-protection",
            Name = "Secrets Protection Policy",
            Description = "Protects API keys and secrets",
            Action = PolicyAction.Block,
            Rules = new List<DlpRule>
            {
                new() { Name = "API Key", Type = DlpRuleType.Regex, Pattern = SensitiveDataPatterns.ApiKey, Sensitivity = SensitivityLevel.Critical },
                new() { Name = "Private Key", Type = DlpRuleType.Regex, Pattern = SensitiveDataPatterns.PrivateKey, Sensitivity = SensitivityLevel.Critical },
                new() { Name = "AWS Access Key", Type = DlpRuleType.Regex, Pattern = SensitiveDataPatterns.AWSAccessKey, Sensitivity = SensitivityLevel.Critical },
                new() { Name = "GitHub Token", Type = DlpRuleType.Regex, Pattern = SensitiveDataPatterns.GitHubToken, Sensitivity = SensitivityLevel.Critical },
                new() { Name = "JWT Token", Type = DlpRuleType.Regex, Pattern = SensitiveDataPatterns.JwtToken, Sensitivity = SensitivityLevel.High }
            }
        };
        _policies[secretsPolicy.Id] = secretsPolicy;
    }

    public async Task<DlpScanResult> ScanContentAsync(string content, CancellationToken cancellationToken = default)
    {
        var result = new DlpScanResult();
        var findings = new List<SensitiveDataFinding>();

        foreach (var policy in _policies.Values.Where(p => p.IsActive))
        {
            foreach (var rule in policy.Rules)
            {
                if (rule.Type == DlpRuleType.Regex)
                {
                    var matches = Regex.Matches(content, rule.Pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        var finding = new SensitiveDataFinding
                        {
                            Type = rule.Name,
                            Value = match.Value,
                            RedactedValue = RedactValue(match.Value, rule.Sensitivity),
                            StartIndex = match.Index,
                            EndIndex = match.Index + match.Length,
                            Sensitivity = rule.Sensitivity,
                            Context = GetContext(content, match.Index, 50)
                        };
                        findings.Add(finding);

                        // Log high-sensitivity findings
                        if (rule.Sensitivity >= SensitivityLevel.High)
                        {
                            await LogDlpIncident(rule.Name, rule.Sensitivity, policy, cancellationToken);
                        }
                    }
                }
            }
        }

        result.Findings = findings;
        result.HasSensitiveData = findings.Any();
        result.FindingCounts = findings.GroupBy(f => f.Type)
            .ToDictionary(g => g.Key, g => g.Count());
        result.Classification = await ClassifyDataAsync(content, cancellationToken);
        result.Summary = GenerateScanSummary(result);

        _logger.LogInformation("DLP scan completed: {FindingCount} findings", findings.Count);
        return result;
    }

    public async Task<DlpScanResult> ScanFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!await _fileSystemService.FileExistsAsync(filePath, cancellationToken))
            throw new FileNotFoundException($"File not found: {filePath}");

        var content = await _fileSystemService.ReadFileAsync(filePath, cancellationToken);
        var result = await ScanContentAsync(content, cancellationToken);

        // Add file context to findings
        var lines = content.Split('\n');
        foreach (var finding in result.Findings)
        {
            finding.FilePath = filePath;
            finding.LineNumber = GetLineNumber(content, finding.StartIndex);
        }

        return result;
    }

    public async Task<DlpScanResult> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var aggregateResult = new DlpScanResult();
        var allFindings = new List<SensitiveDataFinding>();

        var files = await _fileSystemService.GetFilesAsync(directoryPath, "*", true, cancellationToken);
        
        foreach (var file in files)
        {
            try
            {
                var fileResult = await ScanFileAsync(file, cancellationToken);
                allFindings.AddRange(fileResult.Findings);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan file {FilePath}", file);
            }
        }

        aggregateResult.Findings = allFindings;
        aggregateResult.HasSensitiveData = allFindings.Any();
        aggregateResult.FindingCounts = allFindings.GroupBy(f => f.Type)
            .ToDictionary(g => g.Key, g => g.Count());
        aggregateResult.Summary = $"Scanned {files.Count()} files, found {allFindings.Count} sensitive data instances";

        return aggregateResult;
    }

    public Task<DataClassification> ClassifyDataAsync(string content, CancellationToken cancellationToken = default)
    {
        var classification = new DataClassification();
        var categories = new List<string>();
        var scores = new Dictionary<string, double>();

        // Check for various data types
        if (Regex.IsMatch(content, SensitiveDataPatterns.CreditCard) || 
            Regex.IsMatch(content, SensitiveDataPatterns.SSN))
        {
            categories.Add("PII");
            scores["PII"] = 1.0;
            classification.Level = ClassificationLevel.Restricted;
            classification.RequiresEncryption = true;
            classification.RequiresApproval = true;
        }
        else if (Regex.IsMatch(content, SensitiveDataPatterns.ApiKey) ||
                 Regex.IsMatch(content, SensitiveDataPatterns.PrivateKey))
        {
            categories.Add("Secrets");
            scores["Secrets"] = 1.0;
            classification.Level = ClassificationLevel.TopSecret;
            classification.RequiresEncryption = true;
            classification.RequiresApproval = true;
        }
        else if (Regex.IsMatch(content, SensitiveDataPatterns.Email))
        {
            categories.Add("Contact Information");
            scores["Contact Information"] = 0.8;
            classification.Level = ClassificationLevel.Confidential;
        }
        else
        {
            classification.Level = ClassificationLevel.Internal;
        }

        classification.Categories = categories;
        classification.ConfidenceScores = scores;
        classification.HandlingInstructions = GetHandlingInstructions(classification.Level);

        return Task.FromResult(classification);
    }

    public Task<string> RedactSensitiveDataAsync(string content, RedactionLevel level, CancellationToken cancellationToken = default)
    {
        var redacted = content;

        foreach (var policy in _policies.Values.Where(p => p.IsActive))
        {
            foreach (var rule in policy.Rules.Where(r => r.Type == DlpRuleType.Regex))
            {
                redacted = Regex.Replace(redacted, rule.Pattern, match =>
                {
                    return level switch
                    {
                        RedactionLevel.Full => new string('*', match.Value.Length),
                        RedactionLevel.Partial => RedactValue(match.Value, rule.Sensitivity),
                        RedactionLevel.Smart => SmartRedact(match.Value, rule.Name),
                        _ => match.Value
                    };
                }, RegexOptions.IgnoreCase);
            }
        }

        _logger.LogInformation("Redacted sensitive data with {Level} level", level);
        return Task.FromResult(redacted);
    }

    public Task<DlpPolicy> CreatePolicyAsync(DlpPolicy policy, CancellationToken cancellationToken = default)
    {
        if (_policies.ContainsKey(policy.Id))
            throw new InvalidOperationException($"Policy with ID {policy.Id} already exists");

        _policies[policy.Id] = policy;
        _logger.LogInformation("Created DLP policy {PolicyId}", policy.Id);
        
        return Task.FromResult(policy);
    }

    public async Task<bool> ValidateAgainstPolicyAsync(string content, string policyId, CancellationToken cancellationToken = default)
    {
        if (!_policies.TryGetValue(policyId, out var policy))
            throw new InvalidOperationException($"Policy {policyId} not found");

        var scanResult = await ScanContentAsync(content, cancellationToken);
        
        if (!scanResult.HasSensitiveData)
            return true;

        // Check if any findings violate the policy
        foreach (var finding in scanResult.Findings)
        {
            if (policy.Action == PolicyAction.Block && finding.Sensitivity >= SensitivityLevel.High)
            {
                await LogDlpIncident($"Policy violation: {finding.Type}", finding.Sensitivity, policy, cancellationToken);
                return false;
            }
        }

        return true;
    }

    public Task<DlpReport> GenerateDlpReportAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var report = new DlpReport
        {
            PeriodStart = from,
            PeriodEnd = to,
            Incidents = _incidents.Where(i => i.DetectedAt >= from && i.DetectedAt <= to).ToList()
        };

        report.TotalFindings = report.Incidents.Count;
        report.FindingsBySensitivity = report.Incidents
            .GroupBy(i => i.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        report.TopViolators = report.Incidents
            .GroupBy(i => i.UserId)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        report.Statistics["AverageIncidentsPerDay"] = report.TotalFindings / Math.Max(1, (to - from).TotalDays);
        report.Statistics["CriticalIncidents"] = report.Incidents.Count(i => i.Severity == SensitivityLevel.Critical);
        report.Statistics["ResolvedIncidents"] = report.Incidents.Count(i => i.Resolved);

        return Task.FromResult(report);
    }

    private string RedactValue(string value, SensitivityLevel sensitivity)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // For credit cards, we want to show first 2 and last 2 digits for High sensitivity
        if (value.Replace("-", "").Replace(" ", "").Length >= 13)  // Likely a credit card
        {
            var cleanValue = value.Replace("-", "").Replace(" ", "");
            if (cleanValue.All(char.IsDigit) && cleanValue.Length >= 13 && cleanValue.Length <= 19)
            {
                // This is likely a credit card number
                return cleanValue.Substring(0, 2) + new string('*', cleanValue.Length - 4) + cleanValue.Substring(cleanValue.Length - 2);
            }
        }

        return sensitivity switch
        {
            SensitivityLevel.Critical => new string('*', value.Length),
            SensitivityLevel.High => value.Length > 4 
                ? value.Substring(0, 2) + new string('*', value.Length - 4) + value.Substring(value.Length - 2)
                : new string('*', value.Length),
            SensitivityLevel.Medium => value.Length > 6
                ? value.Substring(0, 3) + new string('*', value.Length - 6) + value.Substring(value.Length - 3)
                : new string('*', value.Length),
            _ => value.Substring(0, Math.Min(4, value.Length)) + new string('*', Math.Max(0, value.Length - 4))
        };
    }

    private string SmartRedact(string value, string type)
    {
        return type switch
        {
            "SSN" => "XXX-XX-" + value.Substring(Math.Max(0, value.Length - 4)),
            "Credit Card" => "**** **** **** " + value.Substring(Math.Max(0, value.Length - 4)),
            "Email" => value.Split('@')[0].Substring(0, Math.Min(3, value.Split('@')[0].Length)) + "***@" + value.Split('@')[1],
            "Phone" => "***-***-" + value.Substring(Math.Max(0, value.Length - 4)),
            _ => RedactValue(value, SensitivityLevel.Medium)
        };
    }

    private string GetContext(string content, int index, int contextLength)
    {
        var start = Math.Max(0, index - contextLength);
        var end = Math.Min(content.Length, index + contextLength);
        return content.Substring(start, end - start);
    }

    private int GetLineNumber(string content, int index)
    {
        return content.Substring(0, Math.Min(index, content.Length)).Count(c => c == '\n') + 1;
    }

    private string GenerateScanSummary(DlpScanResult result)
    {
        if (!result.HasSensitiveData)
            return "No sensitive data detected";

        var summary = $"Found {result.Findings.Count} sensitive data instances: ";
        summary += string.Join(", ", result.FindingCounts.Select(kvp => $"{kvp.Value} {kvp.Key}"));
        
        var criticalCount = result.Findings.Count(f => f.Sensitivity == SensitivityLevel.Critical);
        if (criticalCount > 0)
            summary += $" (INCLUDING {criticalCount} CRITICAL)";

        return summary;
    }

    private string GetHandlingInstructions(ClassificationLevel level)
    {
        return level switch
        {
            ClassificationLevel.TopSecret => "Must be encrypted at rest and in transit. Access requires explicit approval.",
            ClassificationLevel.Restricted => "Requires encryption and access logging. Limited distribution.",
            ClassificationLevel.Confidential => "Should be encrypted. Access should be logged.",
            ClassificationLevel.Internal => "For internal use only. Standard security controls apply.",
            _ => "Public information. No special handling required."
        };
    }

    private async Task LogDlpIncident(string description, SensitivityLevel severity, DlpPolicy policy, CancellationToken cancellationToken)
    {
        var incident = new DlpIncident
        {
            Description = description,
            Severity = severity,
            PolicyViolated = policy.Name,
            ActionTaken = policy.Action,
            UserId = "current-user" // Would get from context in real implementation
        };

        _incidents.Add(incident);

        await _auditService.LogAsync(new AuditEntry
        {
            EventType = AuditEventType.DataAccess,
            EventCategory = "DLP",
            EventName = "DLP Violation",
            Description = description,
            Severity = severity switch
            {
                SensitivityLevel.Critical => AuditSeverity.Critical,
                SensitivityLevel.High => AuditSeverity.Error,
                SensitivityLevel.Medium => AuditSeverity.Warning,
                _ => AuditSeverity.Info
            }
        }, cancellationToken);
    }
}