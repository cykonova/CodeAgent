using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models.Security;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Infrastructure.Services;

public class ThreatDetectionService : IThreatDetectionService
{
    private readonly IAuditService _auditService;
    private readonly ILogger<ThreatDetectionService> _logger;
    private readonly Dictionary<string, List<DateTime>> _userActivityHistory = new();
    private readonly Dictionary<string, SecurityIncident> _incidents = new();
    private readonly List<ThreatPattern> _threatPatterns = new();
    private readonly List<string> _maliciousHashes = new();
    private readonly Dictionary<string, int> _failedAttempts = new();

    public ThreatDetectionService(
        IAuditService auditService,
        ILogger<ThreatDetectionService> logger)
    {
        _auditService = auditService;
        _logger = logger;
        InitializeThreatPatterns();
    }

    private void InitializeThreatPatterns()
    {
        // Initialize known threat patterns
        _threatPatterns.AddRange(new[]
        {
            new ThreatPattern
            {
                Name = "SQL Injection",
                Pattern = @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|UNION|ALTER)\b.*\b(FROM|INTO|WHERE)\b)|(--)|(;)|(\|\|)|(\/\*.*\*\/)",
                Severity = ThreatLevel.High,
                Description = "Potential SQL injection attempt detected",
                Mitigations = new List<string> { "Use parameterized queries", "Validate input", "Escape special characters" }
            },
            new ThreatPattern
            {
                Name = "Command Injection",
                Pattern = @"(;|\||&&|\$\(|\`|>|<|\$\{)",
                Severity = ThreatLevel.Critical,
                Description = "Potential command injection attempt detected",
                Mitigations = new List<string> { "Avoid shell execution", "Use safe APIs", "Validate and sanitize input" }
            },
            new ThreatPattern
            {
                Name = "Path Traversal",
                Pattern = @"(\.\.\/|\.\.\\|%2e%2e%2f|%252e%252e%252f)",
                Severity = ThreatLevel.High,
                Description = "Path traversal attempt detected",
                Mitigations = new List<string> { "Validate file paths", "Use canonical paths", "Restrict file access" }
            },
            new ThreatPattern
            {
                Name = "XSS Attack",
                Pattern = @"(<script[^>]*>.*?<\/script>)|(<iframe)|(<object)|(<embed)|(javascript:)|(on\w+\s*=)",
                Severity = ThreatLevel.Medium,
                Description = "Cross-site scripting attempt detected",
                Mitigations = new List<string> { "Encode output", "Use Content Security Policy", "Validate input" }
            }
        });

        // Initialize known malicious hashes (simplified for demo)
        _maliciousHashes.AddRange(new[]
        {
            "5d41402abc4b2a76b9719d911017c592", // Example malware hash
            "098f6bcd4621d373cade4e832627b4f6", // Example malware hash
        });
    }

    public async Task<ThreatAnalysisResult> AnalyzeActivityAsync(string userId, string activity, CancellationToken cancellationToken = default)
    {
        var result = new ThreatAnalysisResult
        {
            ThreatLevel = ThreatLevel.None,
            ConfidenceScore = 0.0
        };

        // Check for known threat patterns
        foreach (var pattern in _threatPatterns)
        {
            if (Regex.IsMatch(activity, pattern.Pattern, RegexOptions.IgnoreCase))
            {
                result.Indicators.Add(new ThreatIndicator
                {
                    Type = pattern.Name,
                    Description = pattern.Description,
                    Severity = (double)pattern.Severity / 4.0,
                    Metadata = new Dictionary<string, object> { ["pattern"] = pattern.Pattern }
                });

                if (pattern.Severity > result.ThreatLevel)
                {
                    result.ThreatLevel = pattern.Severity;
                    result.RecommendedActions = pattern.Mitigations;
                }
            }
        }

        // Track user activity for behavioral analysis
        if (!_userActivityHistory.ContainsKey(userId))
            _userActivityHistory[userId] = new List<DateTime>();
        _userActivityHistory[userId].Add(DateTime.UtcNow);

        // Check for rapid activity (potential automation/bot)
        var recentActivities = _userActivityHistory[userId]
            .Where(a => a > DateTime.UtcNow.AddMinutes(-1))
            .ToList();

        if (recentActivities.Count > 30) // More than 30 actions per minute
        {
            result.Indicators.Add(new ThreatIndicator
            {
                Type = "Rapid Activity",
                Description = "Unusually rapid activity detected, possible automation",
                Severity = 0.6
            });
            result.ThreatLevel = ThreatLevel.Medium;
        }

        result.ConfidenceScore = result.Indicators.Any() ? 0.8 : 0.2;
        result.RequiresImmediateAction = result.ThreatLevel >= ThreatLevel.High;
        result.Summary = GenerateThreatSummary(result);

        if (result.ThreatLevel >= ThreatLevel.High)
        {
            await LogThreatDetection(userId, result, cancellationToken);
        }

        return result;
    }

    public async Task<ThreatAnalysisResult> AnalyzeBehaviorAsync(string userId, TimeSpan period, CancellationToken cancellationToken = default)
    {
        var result = new ThreatAnalysisResult();
        
        // Get user's audit history
        var endDate = DateTime.UtcNow;
        var startDate = endDate.Subtract(period);
        var auditLogs = await _auditService.GetUserAuditLogsAsync(userId, 1000, cancellationToken);
        var relevantLogs = auditLogs.Where(l => l.Timestamp >= startDate).ToList();

        // Analyze patterns
        var failedLogins = relevantLogs.Count(l => l.EventName == "LoginFailure");
        var successfulLogins = relevantLogs.Count(l => l.EventName == "LoginSuccess");
        var fileAccesses = relevantLogs.Count(l => l.EventType == AuditEventType.FileAccess);
        var configChanges = relevantLogs.Count(l => l.EventType == AuditEventType.ConfigurationChange);

        // Check for suspicious patterns
        if (failedLogins > 5)
        {
            result.Indicators.Add(new ThreatIndicator
            {
                Type = "Brute Force",
                Description = $"Multiple failed login attempts ({failedLogins})",
                Severity = 0.7
            });
            result.ThreatLevel = ThreatLevel.High;
        }

        if (fileAccesses > 100)
        {
            result.Indicators.Add(new ThreatIndicator
            {
                Type = "Data Exfiltration",
                Description = $"Excessive file access ({fileAccesses} files)",
                Severity = 0.6
            });
            if (result.ThreatLevel < ThreatLevel.Medium)
                result.ThreatLevel = ThreatLevel.Medium;
        }

        if (configChanges > 10)
        {
            result.Indicators.Add(new ThreatIndicator
            {
                Type = "Configuration Tampering",
                Description = $"Multiple configuration changes ({configChanges})",
                Severity = 0.8
            });
            result.ThreatLevel = ThreatLevel.High;
        }

        // Check time-based anomalies
        var nightTimeActivities = relevantLogs.Count(l => l.Timestamp.Hour < 6 || l.Timestamp.Hour > 22);
        if (nightTimeActivities > relevantLogs.Count * 0.3) // More than 30% at night
        {
            result.Indicators.Add(new ThreatIndicator
            {
                Type = "Unusual Hours",
                Description = "Significant activity during non-business hours",
                Severity = 0.4
            });
        }

        result.ConfidenceScore = CalculateConfidenceScore(result.Indicators);
        result.Summary = GenerateThreatSummary(result);
        result.RequiresImmediateAction = result.ThreatLevel >= ThreatLevel.High;

        return result;
    }

    public Task<ThreatAnalysisResult> ScanForMalwareAsync(string content, CancellationToken cancellationToken = default)
    {
        var result = new ThreatAnalysisResult();

        // Calculate hash of content
        using var sha256 = SHA256.Create();
        var hash = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(content))).Replace("-", "").ToLower();

        // Check against known malicious hashes
        if (_maliciousHashes.Contains(hash))
        {
            result.ThreatLevel = ThreatLevel.Critical;
            result.Indicators.Add(new ThreatIndicator
            {
                Type = "Known Malware",
                Description = "Content matches known malware signature",
                Severity = 1.0,
                Metadata = new Dictionary<string, object> { ["hash"] = hash }
            });
            result.RequiresImmediateAction = true;
        }

        // Check for suspicious patterns
        var suspiciousPatterns = new[]
        {
            @"eval\s*\(",
            @"exec\s*\(",
            @"system\s*\(",
            @"shell_exec",
            @"passthru",
            @"proc_open",
            @"popen",
            @"base64_decode",
            @"str_rot13",
            @"gzinflate",
            @"gzuncompress",
            @"create_function"
        };

        foreach (var pattern in suspiciousPatterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                result.Indicators.Add(new ThreatIndicator
                {
                    Type = "Suspicious Code",
                    Description = $"Suspicious pattern detected: {pattern}",
                    Severity = 0.6
                });
                if (result.ThreatLevel < ThreatLevel.Medium)
                    result.ThreatLevel = ThreatLevel.Medium;
            }
        }

        // Check for obfuscation
        if (DetectObfuscation(content))
        {
            result.Indicators.Add(new ThreatIndicator
            {
                Type = "Obfuscation",
                Description = "Code appears to be obfuscated",
                Severity = 0.5
            });
            if (result.ThreatLevel < ThreatLevel.Medium)
                result.ThreatLevel = ThreatLevel.Medium;
        }

        result.ConfidenceScore = CalculateConfidenceScore(result.Indicators);
        result.Summary = GenerateThreatSummary(result);

        return Task.FromResult(result);
    }

    public async Task<ThreatAnalysisResult> ScanFileForThreatsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // This would integrate with actual file scanning
        // For now, we'll do basic checks
        var result = new ThreatAnalysisResult();

        // Check file extension
        var dangerousExtensions = new[] { ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".jar" };
        var extension = Path.GetExtension(filePath).ToLower();
        
        if (dangerousExtensions.Contains(extension))
        {
            result.Indicators.Add(new ThreatIndicator
            {
                Type = "Potentially Dangerous File",
                Description = $"File has potentially dangerous extension: {extension}",
                Severity = 0.4
            });
            result.ThreatLevel = ThreatLevel.Low;
        }

        // Check file name for suspicious patterns
        var suspiciousNames = new[] { "crack", "keygen", "patch", "loader", "injector", "exploit" };
        var fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
        
        foreach (var suspicious in suspiciousNames)
        {
            if (fileName.Contains(suspicious))
            {
                result.Indicators.Add(new ThreatIndicator
                {
                    Type = "Suspicious Filename",
                    Description = $"Filename contains suspicious term: {suspicious}",
                    Severity = 0.5
                });
                result.ThreatLevel = ThreatLevel.Medium;
                break;
            }
        }

        result.ConfidenceScore = CalculateConfidenceScore(result.Indicators);
        result.Summary = GenerateThreatSummary(result);

        return result;
    }

    public Task<IEnumerable<SecurityIncident>> GetActiveIncidentsAsync(CancellationToken cancellationToken = default)
    {
        var activeIncidents = _incidents.Values
            .Where(i => i.Status != IncidentStatus.Closed && i.Status != IncidentStatus.Resolved)
            .OrderByDescending(i => i.Severity)
            .ThenBy(i => i.CreatedAt)
            .AsEnumerable();

        return Task.FromResult(activeIncidents);
    }

    public async Task<SecurityIncident> ReportIncidentAsync(ThreatLevel level, string description, CancellationToken cancellationToken = default)
    {
        var incident = new SecurityIncident
        {
            Severity = level,
            Title = $"Security Incident - {level}",
            Description = description,
            Status = IncidentStatus.Open
        };

        _incidents[incident.Id] = incident;

        // Log the incident
        await _auditService.LogSecurityEventAsync(
            SecurityEventType.SecurityAlert,
            "system",
            $"Security incident reported: {description}",
            cancellationToken);

        _logger.LogWarning("Security incident reported: {IncidentId} - {Description}", incident.Id, description);

        return incident;
    }

    public Task<bool> RespondToIncidentAsync(string incidentId, IncidentResponse response, CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident))
            return Task.FromResult(false);

        incident.Responses.Add(response);

        // Update incident status based on response
        incident.Status = response.Action switch
        {
            ResponseAction.Investigate => IncidentStatus.InProgress,
            ResponseAction.Contain => IncidentStatus.Contained,
            ResponseAction.Close => IncidentStatus.Closed,
            ResponseAction.Remediate => IncidentStatus.Resolved,
            _ => incident.Status
        };

        if (incident.Status == IncidentStatus.Resolved || incident.Status == IncidentStatus.Closed)
        {
            incident.ResolvedAt = DateTime.UtcNow;
        }

        _logger.LogInformation("Incident {IncidentId} response: {Action}", incidentId, response.Action);

        return Task.FromResult(true);
    }

    public Task<ThreatIntelligence> GetThreatIntelligenceAsync(CancellationToken cancellationToken = default)
    {
        var intelligence = new ThreatIntelligence
        {
            KnownPatterns = _threatPatterns,
            MaliciousHashes = _maliciousHashes,
            BlockedIPs = new List<string> 
            { 
                "192.168.1.100", // Example blocked IPs
                "10.0.0.50"
            },
            SuspiciousUrls = new List<string>
            {
                "http://malicious-site.com",
                "http://phishing-example.com"
            },
            ThreatActors = new Dictionary<string, ThreatLevel>
            {
                ["known-attacker-1"] = ThreatLevel.High,
                ["suspicious-user-2"] = ThreatLevel.Medium
            }
        };

        return Task.FromResult(intelligence);
    }

    public Task<AnomalyDetectionResult> DetectAnomaliesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = new AnomalyDetectionResult
        {
            UserId = userId,
            Anomalies = new List<Anomaly>()
        };

        // Check for login anomalies
        if (_failedAttempts.TryGetValue(userId, out var failures) && failures > 3)
        {
            result.Anomalies.Add(new Anomaly
            {
                Type = "Authentication",
                Description = "Multiple failed login attempts",
                Deviation = failures / 3.0,
                ExpectedBehavior = "Successful login within 3 attempts",
                ObservedBehavior = $"{failures} failed attempts"
            });
        }

        // Check for activity volume anomalies
        if (_userActivityHistory.TryGetValue(userId, out var activities))
        {
            var recentActivity = activities.Count(a => a > DateTime.UtcNow.AddHours(-1));
            if (recentActivity > 50)
            {
                result.Anomalies.Add(new Anomaly
                {
                    Type = "Activity Volume",
                    Description = "Abnormally high activity volume",
                    Deviation = recentActivity / 50.0,
                    ExpectedBehavior = "Normal activity rate",
                    ObservedBehavior = $"{recentActivity} actions in last hour"
                });
            }
        }

        result.AnomalyScore = result.Anomalies.Any() 
            ? result.Anomalies.Average(a => a.Deviation) 
            : 0.0;
        result.RequiresReview = result.AnomalyScore > 0.5;

        return Task.FromResult(result);
    }

    public Task<RiskAssessment> AssessRiskAsync(string userId, string operation, CancellationToken cancellationToken = default)
    {
        var assessment = new RiskAssessment
        {
            UserId = userId,
            Operation = operation,
            Factors = new List<RiskFactor>()
        };

        // Assess user trust level
        var userTrustScore = 1.0;
        if (_failedAttempts.TryGetValue(userId, out var failures))
        {
            userTrustScore = Math.Max(0, 1.0 - (failures * 0.2));
        }

        assessment.Factors.Add(new RiskFactor
        {
            Name = "User Trust",
            Weight = 0.3,
            Score = 1.0 - userTrustScore,
            Description = "Based on user history and authentication"
        });

        // Assess operation risk
        var operationRisk = DetermineOperationRisk(operation);
        assessment.Factors.Add(new RiskFactor
        {
            Name = "Operation Sensitivity",
            Weight = 0.4,
            Score = operationRisk,
            Description = "Risk level of the requested operation"
        });

        // Time-based risk
        var timeRisk = DateTime.UtcNow.Hour < 6 || DateTime.UtcNow.Hour > 22 ? 0.3 : 0.0;
        assessment.Factors.Add(new RiskFactor
        {
            Name = "Time Factor",
            Weight = 0.1,
            Score = timeRisk,
            Description = "Operations during non-business hours"
        });

        // Calculate overall risk
        assessment.RiskScore = assessment.Factors.Sum(f => f.Weight * f.Score);
        assessment.RiskLevel = assessment.RiskScore switch
        {
            < 0.2 => RiskLevel.Negligible,
            < 0.4 => RiskLevel.Low,
            < 0.6 => RiskLevel.Medium,
            < 0.8 => RiskLevel.High,
            _ => RiskLevel.Extreme
        };

        assessment.Approved = assessment.RiskLevel <= RiskLevel.Medium;
        if (!assessment.Approved)
        {
            assessment.ApprovalReason = "Risk level too high for automatic approval";
        }

        return Task.FromResult(assessment);
    }

    private bool DetectObfuscation(string content)
    {
        // Simple obfuscation detection
        var hexPattern = @"\\x[0-9a-fA-F]{2}";
        var unicodePattern = @"\\u[0-9a-fA-F]{4}";
        var base64Pattern = @"[A-Za-z0-9+/]{20,}={0,2}";

        var hexMatches = Regex.Matches(content, hexPattern).Count;
        var unicodeMatches = Regex.Matches(content, unicodePattern).Count;
        var base64Matches = Regex.Matches(content, base64Pattern).Count;

        return hexMatches > 10 || unicodeMatches > 10 || base64Matches > 5;
    }

    private double CalculateConfidenceScore(List<ThreatIndicator> indicators)
    {
        if (!indicators.Any())
            return 0.1;

        var avgSeverity = indicators.Average(i => i.Severity);
        var count = Math.Min(indicators.Count, 5);
        return Math.Min(0.95, avgSeverity * (1 + count * 0.1));
    }

    private string GenerateThreatSummary(ThreatAnalysisResult result)
    {
        if (!result.Indicators.Any())
            return "No threats detected";

        var summary = $"Detected {result.Indicators.Count} threat indicator(s): ";
        summary += string.Join(", ", result.Indicators.Select(i => i.Type));
        summary += $". Threat level: {result.ThreatLevel}";

        return summary;
    }

    private double DetermineOperationRisk(string operation)
    {
        return operation.ToLower() switch
        {
            var op when op.Contains("delete") => 0.8,
            var op when op.Contains("admin") => 0.7,
            var op when op.Contains("config") => 0.6,
            var op when op.Contains("write") => 0.4,
            var op when op.Contains("read") => 0.2,
            _ => 0.3
        };
    }

    private async Task LogThreatDetection(string userId, ThreatAnalysisResult result, CancellationToken cancellationToken)
    {
        await _auditService.LogSecurityEventAsync(
            SecurityEventType.SuspiciousActivity,
            userId,
            result.Summary,
            cancellationToken);
    }
}