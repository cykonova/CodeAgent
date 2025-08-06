using CodeAgent.CLI.Rendering;
using Spectre.Console;

namespace CodeAgent.CLI;

public static class TestMarkdown
{
    public static void RunTest()
    {
        var renderer = new MarkdigRenderer();
        
        var markdown = @"
# Markdown Rendering Test

## Features Supported

Here's a table showing what we support:

| Feature | Status | Notes |
|---------|--------|-------|
| **Headers** | ✅ Supported | H1-H6 with visual hierarchy |
| *Emphasis* | ✅ Supported | Bold and italic text |
| `Code` | ✅ Supported | Inline and block code |
| Lists | ✅ Supported | Bullet and numbered |
| Tables | ✅ Supported | With borders! |

### Code Example

```csharp
public void HelloWorld()
{
    Console.WriteLine(""Hello, World!"");
}
```

## List Example

1. First item
2. Second item
   - Nested bullet
   - Another nested item
3. Third item

That's all for the test!
";

        renderer.Render(AnsiConsole.Console, markdown);
    }
}