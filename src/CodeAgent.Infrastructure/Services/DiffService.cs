using System.Text;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;

namespace CodeAgent.Infrastructure.Services;

public class DiffService : IDiffService
{
    public async Task<DiffResult> GenerateDiffAsync(string originalContent, string modifiedContent, string? fileName = null)
    {
        var result = new DiffResult
        {
            FileName = fileName
        };

        var lines = await CompareLinesAsync(originalContent, modifiedContent);
        result.Lines = lines.ToList();

        foreach (var line in result.Lines)
        {
            switch (line.Type)
            {
                case DiffLineType.Added:
                    result.AddedLines++;
                    break;
                case DiffLineType.Deleted:
                    result.DeletedLines++;
                    break;
                case DiffLineType.Modified:
                    result.ModifiedLines++;
                    break;
            }
        }

        result.UnifiedDiff = await GenerateUnifiedDiffAsync(originalContent, modifiedContent, fileName);

        return result;
    }

    public async Task<string> GenerateUnifiedDiffAsync(string originalContent, string modifiedContent, string? fileName = null)
    {
        return await Task.Run(() =>
        {
            var originalLines = originalContent.Split('\n');
            var modifiedLines = modifiedContent.Split('\n');
            var sb = new StringBuilder();

            // Add unified diff header
            sb.AppendLine($"--- {fileName ?? "original"}");
            sb.AppendLine($"+++ {fileName ?? "modified"}");

            // Simple diff implementation - in production, consider using a library like DiffPlex
            var changes = GetChanges(originalLines, modifiedLines);
            
            foreach (var change in changes)
            {
                sb.AppendLine(change);
            }

            return sb.ToString();
        });
    }

    public async Task<IEnumerable<DiffLine>> CompareLinesAsync(string originalContent, string modifiedContent)
    {
        return await Task.Run(() =>
        {
            var originalLines = originalContent.Split('\n');
            var modifiedLines = modifiedContent.Split('\n');
            var result = new List<DiffLine>();

            // Simple line-by-line comparison
            // In production, consider using a more sophisticated algorithm like Myers diff
            var maxLines = Math.Max(originalLines.Length, modifiedLines.Length);

            for (int i = 0; i < maxLines; i++)
            {
                if (i >= originalLines.Length)
                {
                    // Line added
                    result.Add(new DiffLine
                    {
                        Type = DiffLineType.Added,
                        ModifiedLineNumber = i + 1,
                        Content = modifiedLines[i]
                    });
                }
                else if (i >= modifiedLines.Length)
                {
                    // Line deleted
                    result.Add(new DiffLine
                    {
                        Type = DiffLineType.Deleted,
                        OriginalLineNumber = i + 1,
                        Content = originalLines[i]
                    });
                }
                else if (originalLines[i] != modifiedLines[i])
                {
                    // Line modified - represent as delete + add
                    result.Add(new DiffLine
                    {
                        Type = DiffLineType.Deleted,
                        OriginalLineNumber = i + 1,
                        Content = originalLines[i]
                    });
                    result.Add(new DiffLine
                    {
                        Type = DiffLineType.Added,
                        ModifiedLineNumber = i + 1,
                        Content = modifiedLines[i]
                    });
                }
                else
                {
                    // Line unchanged
                    result.Add(new DiffLine
                    {
                        Type = DiffLineType.Unchanged,
                        OriginalLineNumber = i + 1,
                        ModifiedLineNumber = i + 1,
                        Content = originalLines[i]
                    });
                }
            }

            return result;
        });
    }

    private IEnumerable<string> GetChanges(string[] originalLines, string[] modifiedLines)
    {
        var changes = new List<string>();
        int contextLines = 3;
        var diffLines = new List<(int lineNum, string prefix, string content)>();

        // Build diff representation
        var maxLines = Math.Max(originalLines.Length, modifiedLines.Length);
        for (int i = 0; i < maxLines; i++)
        {
            if (i >= originalLines.Length)
            {
                diffLines.Add((i + 1, "+", modifiedLines[i]));
            }
            else if (i >= modifiedLines.Length)
            {
                diffLines.Add((i + 1, "-", originalLines[i]));
            }
            else if (originalLines[i] != modifiedLines[i])
            {
                diffLines.Add((i + 1, "-", originalLines[i]));
                diffLines.Add((i + 1, "+", modifiedLines[i]));
            }
            else
            {
                diffLines.Add((i + 1, " ", originalLines[i]));
            }
        }

        // Group changes into hunks
        int startLine = -1;
        var currentHunk = new List<string>();
        
        for (int i = 0; i < diffLines.Count; i++)
        {
            var (lineNum, prefix, content) = diffLines[i];
            
            if (prefix != " ")
            {
                if (startLine == -1)
                {
                    startLine = Math.Max(0, lineNum - contextLines);
                    
                    // Add context before
                    for (int j = Math.Max(0, i - contextLines); j < i; j++)
                    {
                        if (j >= 0 && j < diffLines.Count)
                        {
                            currentHunk.Add($" {diffLines[j].content}");
                        }
                    }
                }
                
                currentHunk.Add($"{prefix}{content}");
            }
            else if (startLine != -1)
            {
                // Add context after change
                if (currentHunk.Count > 0 && i - startLine <= contextLines * 2)
                {
                    currentHunk.Add($" {content}");
                }
                else if (currentHunk.Count > 0)
                {
                    // End of hunk
                    changes.Add($"@@ -{startLine},{originalLines.Length} +{startLine},{modifiedLines.Length} @@");
                    changes.AddRange(currentHunk);
                    currentHunk.Clear();
                    startLine = -1;
                }
            }
        }

        // Add final hunk if exists
        if (currentHunk.Count > 0)
        {
            changes.Add($"@@ -{startLine},{originalLines.Length} +{startLine},{modifiedLines.Length} @@");
            changes.AddRange(currentHunk);
        }

        return changes;
    }
}