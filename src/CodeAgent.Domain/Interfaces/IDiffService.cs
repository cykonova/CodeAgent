using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface IDiffService
{
    Task<DiffResult> GenerateDiffAsync(string originalContent, string modifiedContent, string? fileName = null);
    Task<string> GenerateUnifiedDiffAsync(string originalContent, string modifiedContent, string? fileName = null);
    Task<IEnumerable<DiffLine>> CompareLinesAsync(string originalContent, string modifiedContent);
}