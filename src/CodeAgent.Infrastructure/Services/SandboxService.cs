using System.Diagnostics;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models.Security;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Infrastructure.Services;

public class SandboxService : ISandboxService
{
    private readonly ILogger<SandboxService> _logger;
    private readonly IAuditService _auditService;
    private readonly Dictionary<string, SandboxEnvironment> _sandboxes = new();
    private readonly Dictionary<string, Process> _sandboxProcesses = new();
    private readonly Dictionary<string, ResourceUsage> _resourceUsage = new();

    public SandboxService(
        ILogger<SandboxService> logger,
        IAuditService auditService)
    {
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<SandboxEnvironment> CreateSandboxAsync(SandboxConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var sandbox = new SandboxEnvironment
        {
            Name = configuration.Name,
            Type = configuration.Type,
            ResourceLimits = configuration.ResourceLimits,
            NetworkIsolation = configuration.NetworkIsolation,
            Status = SandboxStatus.Creating
        };

        _sandboxes[sandbox.Id] = sandbox;

        try
        {
            // Create sandbox based on type
            switch (configuration.Type)
            {
                case SandboxType.Process:
                    await CreateProcessSandbox(sandbox, configuration, cancellationToken);
                    break;
                case SandboxType.FileSystem:
                    await CreateFileSystemSandbox(sandbox, configuration, cancellationToken);
                    break;
                case SandboxType.Container:
                    // Would integrate with Docker/containerization
                    _logger.LogInformation("Container sandbox creation simulated for {SandboxId}", sandbox.Id);
                    break;
                case SandboxType.WebAssembly:
                    // Would integrate with WASM runtime
                    _logger.LogInformation("WASM sandbox creation simulated for {SandboxId}", sandbox.Id);
                    break;
                default:
                    throw new NotSupportedException($"Sandbox type {configuration.Type} not supported");
            }

            sandbox.Status = SandboxStatus.Ready;
            
            await _auditService.LogAsync(new AuditEntry
            {
                EventType = AuditEventType.SystemEvent,
                EventName = "SandboxCreated",
                Description = $"Created {configuration.Type} sandbox: {sandbox.Name}",
                ResourceId = sandbox.Id,
                ResourceType = "Sandbox"
            }, cancellationToken);

            _logger.LogInformation("Created sandbox {SandboxId} of type {Type}", sandbox.Id, configuration.Type);
        }
        catch (Exception ex)
        {
            sandbox.Status = SandboxStatus.Error;
            _logger.LogError(ex, "Failed to create sandbox {SandboxId}", sandbox.Id);
            throw;
        }

        return sandbox;
    }

    public async Task<ExecutionResult> ExecuteInSandboxAsync(string sandboxId, string code, CancellationToken cancellationToken = default)
    {
        if (!_sandboxes.TryGetValue(sandboxId, out var sandbox))
            throw new InvalidOperationException($"Sandbox {sandboxId} not found");

        if (sandbox.Status != SandboxStatus.Ready && sandbox.Status != SandboxStatus.Running)
            throw new InvalidOperationException($"Sandbox {sandboxId} is not ready for execution");

        var result = new ExecutionResult
        {
            SandboxId = sandboxId
        };

        var stopwatch = Stopwatch.StartNew();
        sandbox.Status = SandboxStatus.Running;

        try
        {
            // Check for security violations before execution
            var violations = await CheckSecurityViolations(code, sandbox);
            if (violations.Any(v => v.Severity >= ViolationSeverity.Error))
            {
                result.Success = false;
                result.SecurityViolations = violations;
                result.ErrorOutput = "Execution blocked due to security violations";
                return result;
            }

            // Execute based on sandbox type
            switch (sandbox.Type)
            {
                case SandboxType.Process:
                    result = await ExecuteInProcessSandbox(sandbox, code, cancellationToken);
                    break;
                case SandboxType.FileSystem:
                    result = await ExecuteInFileSystemSandbox(sandbox, code, cancellationToken);
                    break;
                default:
                    // Simulated execution for other types
                    result.Success = true;
                    result.Output = $"Simulated execution in {sandbox.Type} sandbox";
                    break;
            }

            // Monitor resource usage
            result.ResourceUsage = await GetResourceUsageAsync(sandboxId, cancellationToken);
            
            // Check if resource limits were exceeded
            if (IsResourceLimitExceeded(result.ResourceUsage, sandbox.ResourceLimits))
            {
                result.SecurityViolations.Add(new SecurityViolation
                {
                    Type = "ResourceLimit",
                    Description = "Resource limits exceeded during execution",
                    Severity = ViolationSeverity.Warning
                });
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorOutput = ex.Message;
            _logger.LogError(ex, "Execution failed in sandbox {SandboxId}", sandboxId);
        }
        finally
        {
            sandbox.Status = SandboxStatus.Ready;
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
        }

        await LogExecution(sandbox, result, cancellationToken);
        return result;
    }

    public Task<bool> DestroySandboxAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        if (!_sandboxes.TryGetValue(sandboxId, out var sandbox))
            return Task.FromResult(false);

        sandbox.Status = SandboxStatus.Destroying;

        try
        {
            // Clean up based on sandbox type
            if (_sandboxProcesses.TryGetValue(sandboxId, out var process))
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.Dispose();
                }
                _sandboxProcesses.Remove(sandboxId);
            }

            // Clean up temporary files
            var tempPath = Path.Combine(Path.GetTempPath(), $"sandbox_{sandboxId}");
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            sandbox.Status = SandboxStatus.Destroyed;
            sandbox.DestroyedAt = DateTime.UtcNow;
            _sandboxes.Remove(sandboxId);
            _resourceUsage.Remove(sandboxId);

            _logger.LogInformation("Destroyed sandbox {SandboxId}", sandboxId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to destroy sandbox {SandboxId}", sandboxId);
            sandbox.Status = SandboxStatus.Error;
            return Task.FromResult(false);
        }
    }

    public Task<SandboxStatus> GetSandboxStatusAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        if (!_sandboxes.TryGetValue(sandboxId, out var sandbox))
            return Task.FromResult(SandboxStatus.Destroyed);

        return Task.FromResult(sandbox.Status);
    }

    public Task<ResourceUsage> GetResourceUsageAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        if (!_resourceUsage.TryGetValue(sandboxId, out var usage))
        {
            usage = new ResourceUsage();
        }

        // Update with current measurements (simplified)
        if (_sandboxProcesses.TryGetValue(sandboxId, out var process) && process != null)
        {
            try
            {
                if (!process.HasExited)
                {
                    usage.MemoryUsedBytes = process.WorkingSet64;
                    usage.ActiveProcesses = 1;
                    usage.CpuUsagePercent = CalculateCpuUsage(process);
                }
            }
            catch
            {
                // Process may have exited
            }
        }

        usage.MeasuredAt = DateTime.UtcNow;
        _resourceUsage[sandboxId] = usage;

        return Task.FromResult(usage);
    }

    public Task<bool> SetResourceLimitsAsync(string sandboxId, ResourceLimits limits, CancellationToken cancellationToken = default)
    {
        if (!_sandboxes.TryGetValue(sandboxId, out var sandbox))
            return Task.FromResult(false);

        sandbox.ResourceLimits = limits;
        
        // Apply limits if sandbox is running
        if (sandbox.Status == SandboxStatus.Running && _sandboxProcesses.TryGetValue(sandboxId, out var process))
        {
            try
            {
                // In a real implementation, would use OS-specific APIs to set limits
                // For example, on Linux: cgroups, on Windows: Job Objects
                _logger.LogInformation("Updated resource limits for sandbox {SandboxId}", sandboxId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply resource limits to sandbox {SandboxId}", sandboxId);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    public Task<IEnumerable<SandboxEnvironment>> GetActiveSandboxesAsync(CancellationToken cancellationToken = default)
    {
        var activeSandboxes = _sandboxes.Values
            .Where(s => s.Status != SandboxStatus.Destroyed && s.Status != SandboxStatus.Destroying)
            .OrderBy(s => s.CreatedAt)
            .AsEnumerable();

        return Task.FromResult(activeSandboxes);
    }

    public Task<bool> IsolateSandboxNetworkAsync(string sandboxId, NetworkIsolationLevel level, CancellationToken cancellationToken = default)
    {
        if (!_sandboxes.TryGetValue(sandboxId, out var sandbox))
            return Task.FromResult(false);

        sandbox.NetworkIsolation = level;
        
        // In a real implementation, would configure network isolation
        // For example: iptables rules, network namespaces, etc.
        _logger.LogInformation("Set network isolation level {Level} for sandbox {SandboxId}", level, sandboxId);

        return Task.FromResult(true);
    }

    private async Task CreateProcessSandbox(SandboxEnvironment sandbox, SandboxConfiguration configuration, CancellationToken cancellationToken)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe", // Or "sh" on Unix
            Arguments = "/c echo Sandbox ready",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true
        };

        // Set environment variables
        foreach (var (key, value) in configuration.EnvironmentVariables)
        {
            processInfo.EnvironmentVariables[key] = value;
        }

        var process = new Process { StartInfo = processInfo };
        _sandboxProcesses[sandbox.Id] = process;
        
        await Task.CompletedTask;
    }

    private async Task CreateFileSystemSandbox(SandboxEnvironment sandbox, SandboxConfiguration configuration, CancellationToken cancellationToken)
    {
        // Create isolated filesystem
        var sandboxPath = Path.Combine(Path.GetTempPath(), $"sandbox_{sandbox.Id}");
        Directory.CreateDirectory(sandboxPath);
        
        sandbox.Metadata["RootPath"] = sandboxPath;
        
        _logger.LogInformation("Created filesystem sandbox at {Path}", sandboxPath);
        await Task.CompletedTask;
    }

    private async Task<ExecutionResult> ExecuteInProcessSandbox(SandboxEnvironment sandbox, string code, CancellationToken cancellationToken)
    {
        var result = new ExecutionResult { SandboxId = sandbox.Id };
        
        // This is a simplified implementation
        // In production, would use more sophisticated process isolation
        var processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {code}",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(processInfo);
        if (process != null)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync(cancellationToken);
            
            result.Output = await outputTask;
            result.ErrorOutput = await errorTask;
            result.ExitCode = process.ExitCode;
            result.Success = process.ExitCode == 0;
        }

        return result;
    }

    private async Task<ExecutionResult> ExecuteInFileSystemSandbox(SandboxEnvironment sandbox, string code, CancellationToken cancellationToken)
    {
        var result = new ExecutionResult { SandboxId = sandbox.Id };
        
        if (!sandbox.Metadata.TryGetValue("RootPath", out var rootPathObj) || rootPathObj is not string rootPath)
        {
            result.Success = false;
            result.ErrorOutput = "Sandbox root path not found";
            return result;
        }

        // Write code to sandbox and execute
        var scriptPath = Path.Combine(rootPath, "script.cmd");
        await File.WriteAllTextAsync(scriptPath, code, cancellationToken);
        
        // Execute in isolated environment
        return await ExecuteInProcessSandbox(sandbox, scriptPath, cancellationToken);
    }

    private async Task<List<SecurityViolation>> CheckSecurityViolations(string code, SandboxEnvironment sandbox)
    {
        var violations = new List<SecurityViolation>();
        
        // Check for dangerous commands
        var dangerousPatterns = new[]
        {
            @"rm\s+-rf",
            @"format\s+",
            @"del\s+\/s",
            @"shutdown",
            @"reboot",
            @"kill\s+-9"
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(code, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                violations.Add(new SecurityViolation
                {
                    Type = "DangerousCommand",
                    Description = $"Dangerous command pattern detected: {pattern}",
                    Severity = ViolationSeverity.Error
                });
            }
        }

        return violations;
    }

    private bool IsResourceLimitExceeded(ResourceUsage usage, ResourceLimits limits)
    {
        return usage.MemoryUsedBytes > limits.MaxMemoryBytes ||
               usage.CpuUsagePercent > limits.MaxCpuPercent ||
               usage.DiskUsedBytes > limits.MaxDiskBytes ||
               usage.ActiveProcesses > limits.MaxProcesses ||
               usage.OpenFileHandles > limits.MaxFileHandles;
    }

    private double CalculateCpuUsage(Process process)
    {
        // Simplified CPU usage calculation
        try
        {
            return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / Environment.TickCount * 100;
        }
        catch
        {
            return 0;
        }
    }

    private async Task LogExecution(SandboxEnvironment sandbox, ExecutionResult result, CancellationToken cancellationToken)
    {
        await _auditService.LogAsync(new AuditEntry
        {
            EventType = AuditEventType.SystemEvent,
            EventName = "SandboxExecution",
            Description = $"Executed code in {sandbox.Type} sandbox",
            ResourceId = sandbox.Id,
            ResourceType = "Sandbox",
            Success = result.Success,
            ErrorMessage = result.ErrorOutput,
            Metadata = new Dictionary<string, object>
            {
                ["ExecutionTime"] = result.ExecutionTime.TotalMilliseconds,
                ["ExitCode"] = result.ExitCode,
                ["Violations"] = result.SecurityViolations.Count
            }
        }, cancellationToken);
    }
}