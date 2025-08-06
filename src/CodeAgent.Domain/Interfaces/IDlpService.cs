using CodeAgent.Domain.Models.Security;

namespace CodeAgent.Domain.Interfaces;

public interface IDlpService
{
    Task<DlpScanResult> ScanContentAsync(string content, CancellationToken cancellationToken = default);
    Task<DlpScanResult> ScanFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<DlpScanResult> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
    Task<DataClassification> ClassifyDataAsync(string content, CancellationToken cancellationToken = default);
    Task<string> RedactSensitiveDataAsync(string content, RedactionLevel level, CancellationToken cancellationToken = default);
    Task<DlpPolicy> CreatePolicyAsync(DlpPolicy policy, CancellationToken cancellationToken = default);
    Task<bool> ValidateAgainstPolicyAsync(string content, string policyId, CancellationToken cancellationToken = default);
    Task<DlpReport> GenerateDlpReportAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}