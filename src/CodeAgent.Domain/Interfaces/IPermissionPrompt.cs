namespace CodeAgent.Domain.Interfaces;

public interface IPermissionPrompt
{
    Task<PermissionResult> PromptForPermissionAsync(string operation, string path, string projectDir, string? details = null);
}