using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface IInternalToolService
{
    List<ToolDefinition> GetAvailableTools();
    Task<ToolResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default);
}