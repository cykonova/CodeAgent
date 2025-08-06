namespace CodeAgent.Domain.Interfaces;

public interface IPermissionPrompt
{
    Task<bool> PromptForPermissionAsync(string operation, string path, string? details = null);
}