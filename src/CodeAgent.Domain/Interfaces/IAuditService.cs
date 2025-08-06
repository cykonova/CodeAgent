using CodeAgent.Domain.Models.Security;

namespace CodeAgent.Domain.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    Task LogSecurityEventAsync(SecurityEventType eventType, string userId, string details, CancellationToken cancellationToken = default);
    Task LogFileOperationAsync(FileOperation operation, string filePath, string userId, CancellationToken cancellationToken = default);
    Task LogProviderAccessAsync(string providerName, string userId, string operation, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEntry>> GetAuditLogsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEntry>> GetUserAuditLogsAsync(string userId, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEntry>> SearchAuditLogsAsync(string query, CancellationToken cancellationToken = default);
    Task<AuditReport> GenerateAuditReportAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<ComplianceReport> GenerateComplianceReportAsync(ComplianceStandard standard, CancellationToken cancellationToken = default);
}