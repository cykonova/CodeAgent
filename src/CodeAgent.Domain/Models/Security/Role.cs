namespace CodeAgent.Domain.Models.Security;

public class Role
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Permission> Permissions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    public bool IsSystem { get; set; }
}

public class Permission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public static class SystemRoles
{
    public const string Administrator = "administrator";
    public const string Developer = "developer";
    public const string Reviewer = "reviewer";
    public const string ReadOnly = "readonly";
}

public static class SystemPermissions
{
    // File operations
    public const string FileRead = "file:read";
    public const string FileWrite = "file:write";
    public const string FileDelete = "file:delete";
    
    // Provider operations
    public const string ProviderManage = "provider:manage";
    public const string ProviderUse = "provider:use";
    
    // Security operations
    public const string SecurityManage = "security:manage";
    public const string SecurityAudit = "security:audit";
    
    // Session operations
    public const string SessionCreate = "session:create";
    public const string SessionDelete = "session:delete";
    public const string SessionShare = "session:share";
    
    // Plugin operations
    public const string PluginInstall = "plugin:install";
    public const string PluginManage = "plugin:manage";
    public const string PluginExecute = "plugin:execute";
}