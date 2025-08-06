namespace CodeAgent.Domain.Interfaces;

public interface IContextService
{
    Task AddFileToContextAsync(string filePath, CancellationToken cancellationToken = default);
    Task RemoveFileFromContextAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetContextFilesAsync(CancellationToken cancellationToken = default);
    Task ClearContextAsync(CancellationToken cancellationToken = default);
    Task<string> GetContextSummaryAsync(CancellationToken cancellationToken = default);
    Task<string> BuildPromptContextAsync(string basePrompt, CancellationToken cancellationToken = default);
    Task SaveContextAsync(string name, CancellationToken cancellationToken = default);
    Task LoadContextAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetSavedContextsAsync(CancellationToken cancellationToken = default);
}