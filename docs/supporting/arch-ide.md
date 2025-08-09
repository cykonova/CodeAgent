# IDE Extensions Architecture

## Shared Protocol
```typescript
interface IDEMessage {
  command: string;
  context: {
    file?: string;
    selection?: Range;
    project?: string;
  };
  params: any;
}

interface IDEResponse {
  success: boolean;
  data?: any;
  error?: string;
}
```

## VS Code Extension
```typescript
// Extension API
export function activate(context: vscode.ExtensionContext) {
  // Register commands
  context.subscriptions.push(
    vscode.commands.registerCommand('codeagent.plan', planCommand),
    vscode.commands.registerCommand('codeagent.code', codeCommand)
  );
  
  // Register providers
  vscode.languages.registerInlineCompletionItemProvider(
    { pattern: '**/*' },
    new CodeAgentCompletionProvider()
  );
  
  // Create webview
  const panel = vscode.window.createWebviewPanel(
    'codeAgent',
    'Code Agent',
    vscode.ViewColumn.Two
  );
}
```

## Visual Studio Extension
```csharp
[Command(PackageIds.CodeAgentCommand)]
internal sealed class CodeAgentCommand : BaseCommand<CodeAgentCommand>
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        var dte = await VS.GetServiceAsync<DTE>();
        var selection = dte.ActiveDocument.Selection;
        
        var client = new CodeAgentClient();
        var response = await client.ProcessCommand(selection.Text);
        
        await VS.MessageBox.ShowAsync(response);
    }
}
```

## JetBrains Plugin
```java
public class CodeAgentAction extends AnAction {
    @Override
    public void actionPerformed(@NotNull AnActionEvent e) {
        Project project = e.getProject();
        Editor editor = e.getData(CommonDataKeys.EDITOR);
        
        CodeAgentService service = project.getService(CodeAgentService.class);
        service.processSelection(editor.getSelectionModel().getSelectedText());
    }
}
```

## Communication Layer
- WebSocket connection to server
- Local API fallback
- Message queuing
- Response caching