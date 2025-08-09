namespace CodeAgent.Sandbox.Models;

public class SandboxExecutionResult
{
    public int      ExitCode       { get; set; }
    public string   StandardOutput { get; set; } = string.Empty;
    public string   StandardError  { get; set; } = string.Empty;
    public TimeSpan Duration       { get; set; }
}