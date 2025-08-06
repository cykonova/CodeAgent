using CodeAgent.Domain.Interfaces;
using System.Text;
using System.Text.Json;

namespace CodeAgent.Web.Services;

public class ExportService : IExportService
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        ISessionService sessionService,
        ILogger<ExportService> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<byte[]> ExportSessionAsync(string sessionId, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var session = await _sessionService.LoadSessionAsync(sessionId, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        return format switch
        {
            ExportFormat.Json => ExportToJson(session),
            ExportFormat.Markdown => ExportToMarkdown(session),
            ExportFormat.Html => ExportToHtml(session),
            ExportFormat.Csv => ExportToCsv(session),
            _ => throw new NotSupportedException($"Format {format} not supported for session export")
        };
    }

    public Task<byte[]> ExportConfigurationAsync(string profileId, ExportFormat format, CancellationToken cancellationToken = default)
    {
        // Simplified implementation
        var config = new
        {
            ProfileId = profileId,
            ExportedAt = DateTime.UtcNow,
            Settings = new Dictionary<string, object>
            {
                ["theme"] = "dark",
                ["autoSave"] = true,
                ["defaultProvider"] = "openai"
            }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(Encoding.UTF8.GetBytes(json));
    }

    public Task<byte[]> ExportAnalyticsAsync(DateTime from, DateTime to, ExportFormat format, CancellationToken cancellationToken = default)
    {
        // Simplified implementation
        var analytics = new
        {
            PeriodStart = from,
            PeriodEnd = to,
            TotalEvents = 1000,
            UniqueUsers = 50,
            TopFeatures = new[]
            {
                new { Name = "Chat", Count = 500 },
                new { Name = "FileEdit", Count = 300 },
                new { Name = "Search", Count = 200 }
            }
        };

        var json = JsonSerializer.Serialize(analytics, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(Encoding.UTF8.GetBytes(json));
    }

    public async Task<string> ImportSessionAsync(byte[] data, ExportFormat format, CancellationToken cancellationToken = default)
    {
        if (format != ExportFormat.Json)
            throw new NotSupportedException($"Format {format} not supported for session import");

        var json = Encoding.UTF8.GetString(data);
        var session = JsonSerializer.Deserialize<Domain.Models.Session>(json);
        
        if (session == null)
            throw new InvalidOperationException("Invalid session data");

        // Generate new session ID for imported session
        session.Id = Guid.NewGuid().ToString();
        await _sessionService.CreateSessionAsync(session.Name ?? "Imported Session", cancellationToken);
        
        _logger.LogInformation("Imported session {SessionId}", session.Id);
        return session.Id;
    }

    public Task<string> ImportConfigurationAsync(byte[] data, ExportFormat format, CancellationToken cancellationToken = default)
    {
        if (format != ExportFormat.Json)
            throw new NotSupportedException($"Format {format} not supported for configuration import");

        var json = Encoding.UTF8.GetString(data);
        var configData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        
        if (configData == null)
            throw new InvalidOperationException("Invalid configuration data");

        var profileId = Guid.NewGuid().ToString();
        // Would save configuration to storage
        
        _logger.LogInformation("Imported configuration profile {ProfileId}", profileId);
        return Task.FromResult(profileId);
    }

    public Task<ExportMetadata> GetExportMetadataAsync(byte[] data, ExportFormat format, CancellationToken cancellationToken = default)
    {
        if (format != ExportFormat.Json)
        {
            return Task.FromResult(new ExportMetadata
            {
                Type = "Unknown",
                Version = "1.0",
                ExportedAt = DateTime.UtcNow
            });
        }

        try
        {
            var json = Encoding.UTF8.GetString(data);
            using var doc = JsonDocument.Parse(json);
            
            var metadata = new ExportMetadata
            {
                Type = doc.RootElement.TryGetProperty("type", out var type) ? type.GetString() ?? "Unknown" : "Unknown",
                Version = doc.RootElement.TryGetProperty("version", out var version) ? version.GetString() ?? "1.0" : "1.0",
                ExportedAt = doc.RootElement.TryGetProperty("exportedAt", out var exportedAt) ? exportedAt.GetDateTime() : DateTime.UtcNow,
                ExportedBy = doc.RootElement.TryGetProperty("exportedBy", out var exportedBy) ? exportedBy.GetString() ?? "Unknown" : "Unknown"
            };

            return Task.FromResult(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse export metadata");
            return Task.FromResult(new ExportMetadata { Type = "Invalid" });
        }
    }

    private byte[] ExportToJson(Domain.Models.Session session)
    {
        var exportData = new
        {
            type = "session",
            version = "1.0",
            exportedAt = DateTime.UtcNow,
            exportedBy = "current-user",
            session
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }

    private byte[] ExportToMarkdown(Domain.Models.Session session)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine($"# Session: {session.Name}");
        markdown.AppendLine($"**Created:** {session.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        markdown.AppendLine($"**Messages:** {session.Messages.Count}");
        markdown.AppendLine();
        markdown.AppendLine("## Conversation");
        markdown.AppendLine();

        foreach (var message in session.Messages)
        {
            markdown.AppendLine($"### {message.Role} ({message.Timestamp:HH:mm:ss})");
            markdown.AppendLine(message.Content);
            markdown.AppendLine();
        }

        return Encoding.UTF8.GetBytes(markdown.ToString());
    }

    private byte[] ExportToHtml(Domain.Models.Session session)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine($"<title>Session: {session.Name}</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".message { margin: 10px 0; padding: 10px; border-radius: 5px; }");
        html.AppendLine(".user { background-color: #e3f2fd; }");
        html.AppendLine(".assistant { background-color: #f5f5f5; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine($"<h1>Session: {session.Name}</h1>");
        html.AppendLine($"<p>Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");

        foreach (var message in session.Messages)
        {
            var cssClass = message.Role.ToLower();
            html.AppendLine($"<div class='message {cssClass}'>");
            html.AppendLine($"<strong>{message.Role}</strong> ({message.Timestamp:HH:mm:ss})");
            html.AppendLine($"<p>{System.Web.HttpUtility.HtmlEncode(message.Content)}</p>");
            html.AppendLine("</div>");
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return Encoding.UTF8.GetBytes(html.ToString());
    }

    private byte[] ExportToCsv(Domain.Models.Session session)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,Role,Content");

        foreach (var message in session.Messages)
        {
            var content = message.Content.Replace("\"", "\"\"");
            csv.AppendLine($"{message.Timestamp:yyyy-MM-dd HH:mm:ss},{message.Role},\"{content}\"");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}