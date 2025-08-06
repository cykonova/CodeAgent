namespace CodeAgent.Domain.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportSessionAsync(string sessionId, ExportFormat format, CancellationToken cancellationToken = default);
    Task<byte[]> ExportConfigurationAsync(string profileId, ExportFormat format, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAnalyticsAsync(DateTime from, DateTime to, ExportFormat format, CancellationToken cancellationToken = default);
    Task<string> ImportSessionAsync(byte[] data, ExportFormat format, CancellationToken cancellationToken = default);
    Task<string> ImportConfigurationAsync(byte[] data, ExportFormat format, CancellationToken cancellationToken = default);
    Task<ExportMetadata> GetExportMetadataAsync(byte[] data, ExportFormat format, CancellationToken cancellationToken = default);
}

public enum ExportFormat
{
    Json,
    Markdown,
    Html,
    Pdf,
    Csv,
    Xml
}

public class ExportMetadata
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime ExportedAt { get; set; }
    public string ExportedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}