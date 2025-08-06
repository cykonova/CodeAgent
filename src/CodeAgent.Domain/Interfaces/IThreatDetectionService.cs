using CodeAgent.Domain.Models.Security;

namespace CodeAgent.Domain.Interfaces;

public interface IThreatDetectionService
{
    Task<ThreatAnalysisResult> AnalyzeActivityAsync(string userId, string activity, CancellationToken cancellationToken = default);
    Task<ThreatAnalysisResult> AnalyzeBehaviorAsync(string userId, TimeSpan period, CancellationToken cancellationToken = default);
    Task<ThreatAnalysisResult> ScanForMalwareAsync(string content, CancellationToken cancellationToken = default);
    Task<ThreatAnalysisResult> ScanFileForThreatsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityIncident>> GetActiveIncidentsAsync(CancellationToken cancellationToken = default);
    Task<SecurityIncident> ReportIncidentAsync(ThreatLevel level, string description, CancellationToken cancellationToken = default);
    Task<bool> RespondToIncidentAsync(string incidentId, IncidentResponse response, CancellationToken cancellationToken = default);
    Task<ThreatIntelligence> GetThreatIntelligenceAsync(CancellationToken cancellationToken = default);
    Task<AnomalyDetectionResult> DetectAnomaliesAsync(string userId, CancellationToken cancellationToken = default);
    Task<RiskAssessment> AssessRiskAsync(string userId, string operation, CancellationToken cancellationToken = default);
}