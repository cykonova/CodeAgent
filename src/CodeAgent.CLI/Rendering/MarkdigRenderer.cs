using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using System.Text;
using MarkdigTable = Markdig.Extensions.Tables.Table;
using MarkdigTableRow = Markdig.Extensions.Tables.TableRow;
using MarkdigTableCell = Markdig.Extensions.Tables.TableCell;
using SpectreTable = Spectre.Console.Table;

namespace CodeAgent.CLI.Rendering;

/// <summary>
/// Renders Markdown to Spectre.Console using Markdig for parsing
/// </summary>
public class MarkdigRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdigRenderer()
    {
        // Configure Markdig with common extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Includes tables, pipe tables, grid tables, etc.
            .UseEmphasisExtras()     // Strikethrough, subscript, superscript
            .UseTaskLists()          // GitHub-style task lists
            .UseAutoLinks()          // Automatic URL detection
            .Build();
    }

    public void Render(IAnsiConsole console, string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return;

        var document = Markdown.Parse(markdown, _pipeline);
        RenderBlock(console, document);
    }

    private void RenderBlock(IAnsiConsole console, MarkdownObject block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                RenderHeading(console, heading);
                break;

            case ParagraphBlock paragraph:
                RenderParagraph(console, paragraph);
                console.WriteLine();
                break;

            case CodeBlock codeBlock:
                RenderCodeBlock(console, codeBlock);
                break;

            case ListBlock list:
                RenderList(console, list);
                console.WriteLine();
                break;

            case QuoteBlock quote:
                RenderQuote(console, quote);
                break;

            case MarkdigTable table:
                RenderTable(console, table);
                break;

            case ThematicBreakBlock:
                console.Write(new Rule().RuleStyle(Style.Parse("dim")));
                console.WriteLine();
                break;

            case ContainerBlock container:
                foreach (var child in container)
                {
                    RenderBlock(console, child);
                }
                break;
        }
    }

    private void RenderHeading(IAnsiConsole console, HeadingBlock heading)
    {
        var text = ExtractText(heading.Inline);
        
        switch (heading.Level)
        {
            case 1:
                console.WriteLine();
                console.Write(new Rule($"[bold blue]{Markup.Escape(text)}[/]")
                    .RuleStyle(Style.Parse("blue")));
                console.WriteLine();
                break;
            case 2:
                console.WriteLine();
                console.MarkupLine($"[bold cyan]{Markup.Escape(text)}[/]");
                console.MarkupLine("[dim]" + new string('─', Math.Min(text.Length, 50)) + "[/]");
                break;
            case 3:
                console.WriteLine();
                console.MarkupLine($"[bold green]{Markup.Escape(text)}[/]");
                break;
            default:
                console.MarkupLine($"[bold]{Markup.Escape(text)}[/]");
                break;
        }
    }

    private void RenderParagraph(IAnsiConsole console, ParagraphBlock paragraph)
    {
        var markup = RenderInline(paragraph.Inline);
        if (!string.IsNullOrWhiteSpace(markup))
            console.MarkupLine(markup);
    }

    private void RenderCodeBlock(IAnsiConsole console, CodeBlock codeBlock)
    {
        var code = ExtractCodeBlockText(codeBlock);
        var language = (codeBlock as FencedCodeBlock)?.Info ?? "code";

        console.WriteLine();
        var panel = new Panel(new Text(code))
            .Header($"[yellow]{language}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey);
        console.Write(panel);
        console.WriteLine();
    }

    private void RenderList(IAnsiConsole console, ListBlock list)
    {
        foreach (ListItemBlock item in list)
        {
            var indent = new string(' ', (item.Column / 2) * 2);
            var bullet = list.IsOrdered ? $"[blue]{item.Order}.[/]" : "[blue]•[/]";
            
            var firstLine = true;
            foreach (var block in item)
            {
                if (block is ParagraphBlock para)
                {
                    var text = RenderInline(para.Inline);
                    if (firstLine)
                    {
                        console.MarkupLine($"{indent}{bullet} {text}");
                        firstLine = false;
                    }
                    else
                    {
                        console.MarkupLine($"{indent}  {text}");
                    }
                }
                else if (block is ListBlock subList)
                {
                    RenderList(console, subList);
                }
            }
        }
    }

    private void RenderQuote(IAnsiConsole console, QuoteBlock quote)
    {
        console.WriteLine();
        var panel = new Panel(new Text(ExtractText(quote)))
        {
            Border = BoxBorder.Ascii,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(2, 0)
        };
        console.Write(panel);
        console.WriteLine();
    }

    private void RenderTable(IAnsiConsole console, MarkdigTable markdigTable)
    {
        console.WriteLine();
        
        var table = new SpectreTable();
        table.Border(TableBorder.Rounded);

        // Add columns from header
        var headerRow = markdigTable.FirstOrDefault() as MarkdigTableRow;
        if (headerRow != null)
        {
            foreach (MarkdigTableCell cell in headerRow)
            {
                var headerText = ExtractText(cell);
                table.AddColumn(new TableColumn(Markup.Escape(headerText)));
            }
        }

        // Add data rows (skip first row which is header)
        bool isFirst = true;
        foreach (MarkdigTableRow row in markdigTable)
        {
            if (isFirst)
            {
                isFirst = false;
                continue; // Skip header row
            }
            
            var cells = new List<string>();
            foreach (MarkdigTableCell cell in row)
            {
                // Table cells contain blocks, not inlines directly
                var cellText = ExtractText(cell);
                cells.Add(Markup.Escape(cellText));
            }
            
            if (cells.Count > 0)
                table.AddRow(cells.ToArray());
        }

        console.Write(table);
        console.WriteLine();
    }

    private string RenderInline(ContainerInline? inline)
    {
        if (inline == null)
            return string.Empty;

        var sb = new StringBuilder();
        
        foreach (var child in inline)
        {
            sb.Append(RenderInline(child));
        }
        
        return sb.ToString();
    }

    private string RenderInline(Inline inline)
    {
        switch (inline)
        {
            case LiteralInline literal:
                return Markup.Escape(literal.Content.ToString());

            case EmphasisInline emphasis:
                var emphasisContent = RenderInline(emphasis);
                if (emphasis.DelimiterCount == 2)
                    return $"[bold]{emphasisContent}[/]";
                else
                    return $"[italic]{emphasisContent}[/]";

            case CodeInline code:
                return $"[grey on grey19]{Markup.Escape(code.Content)}[/]";

            case LinkInline link:
                var linkText = RenderInline(link);
                var url = link.Url ?? "";
                return $"[link]{linkText}[/] [dim]({Markup.Escape(url)})[/]";

            case LineBreakInline:
                return "\n";

            case HtmlInline:
            case HtmlEntityInline:
                return ""; // Skip HTML for now

            case ContainerInline container:
                return RenderInline(container);

            default:
                return Markup.Escape(inline.ToString() ?? "");
        }
    }

    private string ExtractText(MarkdigTableCell cell)
    {
        var sb = new StringBuilder();
        foreach (var child in cell)
        {
            if (child is ParagraphBlock para)
            {
                sb.Append(ExtractText(para.Inline));
            }
        }
        return sb.ToString().Trim();
    }

    private string ExtractText(ContainerBlock block)
    {
        var sb = new StringBuilder();
        foreach (var child in block)
        {
            if (child is ParagraphBlock para)
            {
                sb.AppendLine(ExtractText(para.Inline));
            }
            else if (child is LeafBlock leaf)
            {
                if (leaf.Lines.Count > 0)
                {
                    foreach (var line in leaf.Lines.Lines)
                    {
                        sb.AppendLine(line.ToString());
                    }
                }
            }
        }
        return sb.ToString().TrimEnd();
    }

    private string ExtractText(ContainerInline? inline)
    {
        if (inline == null)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var child in inline)
        {
            sb.Append(ExtractText(child));
        }
        return sb.ToString();
    }

    private string ExtractText(Inline inline)
    {
        switch (inline)
        {
            case LiteralInline literal:
                return literal.Content.ToString();
            case CodeInline code:
                return code.Content;
            case ContainerInline container:
                return ExtractText(container);
            default:
                return inline.ToString() ?? "";
        }
    }

    private string ExtractCodeBlockText(CodeBlock codeBlock)
    {
        if (codeBlock is FencedCodeBlock fenced)
        {
            if (fenced.Lines.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var line in fenced.Lines.Lines)
                {
                    sb.AppendLine(line.ToString());
                }
                return sb.ToString().TrimEnd();
            }
        }
        
        // For non-fenced code blocks, extract line by line
        if (codeBlock.Lines.Count > 0)
        {
            var sb = new StringBuilder();
            foreach (var line in codeBlock.Lines.Lines)
            {
                sb.AppendLine(line.ToString());
            }
            return sb.ToString().TrimEnd();
        }
        
        return "";
    }
}