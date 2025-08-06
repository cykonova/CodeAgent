using System.Text.RegularExpressions;
using Spectre.Console;

namespace CodeAgent.CLI.Rendering;

public class MarkdownRenderer
{
    private static readonly Regex CodeBlockRegex = new(@"```(\w+)?\n(.*?)```", RegexOptions.Singleline);
    private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`");
    private static readonly Regex BoldRegex = new(@"\*\*([^*]+)\*\*");
    private static readonly Regex ItalicRegex = new(@"\*([^*]+)\*");
    private static readonly Regex HeaderRegex = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline);
    private static readonly Regex ListItemRegex = new(@"^(\s*)[-*+]\s+(.+)$", RegexOptions.Multiline);
    private static readonly Regex NumberedListRegex = new(@"^(\s*)(\d+)\.\s+(.+)$", RegexOptions.Multiline);
    private static readonly Regex LinkRegex = new(@"\[([^\]]+)\]\(([^)]+)\)");

    public void Render(IAnsiConsole console, string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return;
        }

        // Process code blocks first
        var processedText = markdown;
        var codeBlocks = new List<(string original, string replacement)>();
        
        var matches = CodeBlockRegex.Matches(processedText);
        int blockIndex = 0;
        foreach (Match match in matches)
        {
            var language = match.Groups[1].Value;
            var code = match.Groups[2].Value.TrimEnd();
            var placeholder = $"__CODEBLOCK_{blockIndex}__";
            codeBlocks.Add((placeholder, RenderCodeBlock(language, code)));
            processedText = processedText.Replace(match.Value, placeholder);
            blockIndex++;
        }

        // Split by paragraphs
        var paragraphs = processedText.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var paragraph in paragraphs)
        {
            var lines = paragraph.Split('\n');
            
            foreach (var line in lines)
            {
                var processedLine = line;

                // Check for headers
                var headerMatch = HeaderRegex.Match(processedLine);
                if (headerMatch.Success)
                {
                    var level = headerMatch.Groups[1].Value.Length;
                    var text = headerMatch.Groups[2].Value;
                    RenderHeader(console, level, text);
                    continue;
                }

                // Check for list items
                var listMatch = ListItemRegex.Match(processedLine);
                if (listMatch.Success)
                {
                    var indent = listMatch.Groups[1].Value.Length;
                    var text = listMatch.Groups[2].Value;
                    console.MarkupLine($"{new string(' ', indent)}[blue]•[/] {ProcessInlineMarkdown(text)}");
                    continue;
                }

                // Check for numbered lists
                var numberedMatch = NumberedListRegex.Match(processedLine);
                if (numberedMatch.Success)
                {
                    var indent = numberedMatch.Groups[1].Value.Length;
                    var number = numberedMatch.Groups[2].Value;
                    var text = numberedMatch.Groups[3].Value;
                    console.MarkupLine($"{new string(' ', indent)}[blue]{number}.[/] {ProcessInlineMarkdown(text)}");
                    continue;
                }

                // Check for code block placeholders
                bool foundCodeBlock = false;
                foreach (var (placeholder, rendered) in codeBlocks)
                {
                    if (processedLine.Contains(placeholder))
                    {
                        console.WriteLine();
                        console.Write(new Panel(new Text(rendered))
                            .Header("[yellow]Code[/]")
                            .Border(BoxBorder.Rounded)
                            .BorderColor(Color.Grey));
                        console.WriteLine();
                        foundCodeBlock = true;
                        break;
                    }
                }

                if (!foundCodeBlock && !string.IsNullOrWhiteSpace(processedLine))
                {
                    // Regular paragraph text
                    console.MarkupLine(ProcessInlineMarkdown(processedLine));
                }
            }
        }
    }

    private string ProcessInlineMarkdown(string text)
    {
        // Process inline code
        text = InlineCodeRegex.Replace(text, "[grey on grey19]$1[/]");
        
        // Process bold
        text = BoldRegex.Replace(text, "[bold]$1[/]");
        
        // Process italic
        text = ItalicRegex.Replace(text, "[italic]$1[/]");
        
        // Process links
        text = LinkRegex.Replace(text, "[link]$1[/] ([dim]$2[/])");

        // Escape any remaining square brackets for Spectre.Console
        text = text.Replace("[", "[[").Replace("]", "]]");
        
        // Restore our markup
        text = Regex.Replace(text, @"\[\[(/?\w+(?:\s+\w+)*)\]\]", "[$1]");

        return text;
    }

    private void RenderHeader(IAnsiConsole console, int level, string text)
    {
        var processedText = ProcessInlineMarkdown(text);
        
        switch (level)
        {
            case 1:
                console.WriteLine();
                console.Write(new Rule($"[bold blue]{processedText}[/]").RuleStyle(Style.Parse("blue")));
                console.WriteLine();
                break;
            case 2:
                console.WriteLine();
                console.MarkupLine($"[bold cyan]{processedText}[/]");
                console.MarkupLine("[dim]" + new string('─', Math.Min(processedText.Length, 50)) + "[/]");
                break;
            case 3:
                console.WriteLine();
                console.MarkupLine($"[bold green]{processedText}[/]");
                break;
            default:
                console.MarkupLine($"[bold]{processedText}[/]");
                break;
        }
    }

    private string RenderCodeBlock(string language, string code)
    {
        // For now, return the code as-is
        // In a more advanced implementation, we could add syntax highlighting
        return code;
    }
}