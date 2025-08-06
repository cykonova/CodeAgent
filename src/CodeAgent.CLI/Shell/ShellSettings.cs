namespace CodeAgent.CLI.Shell;

public class ShellSettings
{
    public string ShellPrompt { get; set; } = "CodeAgent$ ";
    public List<string> History { get; set; } = new();
    public string HistoryFilePath { get; set; } = string.Empty;
    public bool ShowHints { get; set; } = true;
    public bool AutoComplete { get; set; } = true;
    public bool EnableChat { get; set; } = true;
    public string CommandPrefix { get; set; } = "/";
}