using CodeAgent.Domain.Interfaces;

namespace CodeAgent.Web.Services;

public class WebPermissionPrompt : IPermissionPrompt
{
    public Task<PermissionResult> PromptForPermissionAsync(string operation, string path, string projectDir, string? details = null)
    {
        // In web context, we auto-approve since user initiated the action via UI
        // Real permission prompting happens client-side in the browser
        return Task.FromResult(PermissionResult.Allowed);
    }
}