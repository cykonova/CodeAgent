using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CodeAgent.Sandbox.Configuration;
using CodeAgent.Sandbox.Models;
using CodeAgent.Sandbox.Permissions;

namespace CodeAgent.Sandbox.Services;

public class DockerSandboxManager : ISandboxManager
{
    private readonly DockerClient _dockerClient;
    private readonly ILogger<DockerSandboxManager> _logger;
    private readonly SandboxOptions _options;
    private readonly IPermissionProxy _permissionProxy;
    private readonly Dictionary<string, SandboxInstance> _sandboxes = new();

    public DockerSandboxManager(
        ILogger<DockerSandboxManager> logger,
        IOptions<SandboxOptions>      options,
        IPermissionProxy               permissionProxy)
    {
        _logger          = logger;
        _options         = options.Value;
        _permissionProxy = permissionProxy;
        
        _dockerClient = new DockerClientConfiguration()
            .CreateClient();
    }

    public async Task<SandboxInstance> CreateSandboxAsync(SandboxCreateRequest request, CancellationToken cancellationToken = default)
    {
        var sandboxId = Guid.NewGuid().ToString("N");
        var workspacePath = Path.Combine(_options.WorkspaceBasePath, sandboxId);

        // Create workspace directory
        Directory.CreateDirectory(workspacePath);

        var sandbox = new SandboxInstance
        {
            Id            = sandboxId,
            Name          = request.Name,
            WorkspacePath = workspacePath,
            Status        = SandboxStatus.Created,
            CreatedAt     = DateTime.UtcNow,
            Environment   = request.Environment ?? new Dictionary<string, string>()
        };

        // Configure container creation
        var createParams = new CreateContainerParameters
        {
            Name         = $"sandbox-{sandboxId}",
            Image        = _options.DockerImage,
            Env          = sandbox.Environment.Select(kvp => $"{kvp.Key}={kvp.Value}").ToList(),
            HostConfig   = new HostConfig
            {
                Memory       = _options.Resources.MemoryLimitMB * 1024 * 1024,
                NanoCPUs     = (long)(_options.Resources.CpuLimit * 1_000_000_000),
                PidsLimit    = _options.Resources.MaxProcesses,
                Binds        = new List<string> { $"{workspacePath}:/workspace:rw" },
                AutoRemove   = false,
                NetworkMode  = _options.Network.AllowExternal ? "bridge" : "none",
                PortBindings = new Dictionary<string, IList<PortBinding>>()
            },
            WorkingDir   = "/workspace",
            AttachStdin  = true,
            AttachStdout = true,
            AttachStderr = true,
            Tty          = true
        };

        // Configure port mappings if requested
        if (request.RequiredPorts != null)
        {
            foreach (var port in request.RequiredPorts)
            {
                if (_options.Network.ExposedPorts.Contains(port))
                {
                    var portStr = $"{port}/tcp";
                    createParams.ExposedPorts ??= new Dictionary<string, EmptyStruct>();
                    createParams.ExposedPorts[portStr] = default;
                    
                    createParams.HostConfig.PortBindings[portStr] = new List<PortBinding>
                    {
                        new PortBinding { HostIP = "127.0.0.1", HostPort = "0" }
                    };
                }
            }
        }

        try
        {
            // Pull image if not exists
            await PullImageIfNeededAsync(_options.DockerImage, cancellationToken);

            // Create container
            var response = await _dockerClient.Containers.CreateContainerAsync(
                createParams,
                cancellationToken);

            sandbox.ContainerId = response.ID;
            _sandboxes[sandboxId] = sandbox;

            _logger.LogInformation("Created sandbox {SandboxId} with container {ContainerId}", sandboxId, response.ID);
            
            return sandbox;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sandbox {SandboxId}", sandboxId);
            sandbox.Status = SandboxStatus.Failed;
            throw;
        }
    }

    public async Task<SandboxInstance> GetSandboxAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        if (!_sandboxes.TryGetValue(sandboxId, out var sandbox))
        {
            throw new InvalidOperationException($"Sandbox {sandboxId} not found");
        }

        // Update status from Docker
        try
        {
            var container = await _dockerClient.Containers.InspectContainerAsync(sandbox.ContainerId, cancellationToken);
            sandbox.Status = MapDockerStatus(container.State);
            
            // Update port mappings
            if (container.NetworkSettings?.Ports != null)
            {
                sandbox.PortMappings.Clear();
                foreach (var port in container.NetworkSettings.Ports)
                {
                    if (port.Value != null && port.Value.Any())
                    {
                        var containerPort = int.Parse(port.Key.Split('/')[0]);
                        var hostPort = int.Parse(port.Value.First().HostPort);
                        sandbox.PortMappings[containerPort] = hostPort;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inspect container {ContainerId}", sandbox.ContainerId);
        }

        return sandbox;
    }

    public async Task<IEnumerable<SandboxInstance>> ListSandboxesAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _sandboxes.Values.Select(s => GetSandboxAsync(s.Id, cancellationToken));
        return await Task.WhenAll(tasks);
    }

    public async Task StartSandboxAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        var sandbox = await GetSandboxAsync(sandboxId, cancellationToken);
        
        if (sandbox.Status == SandboxStatus.Running)
        {
            return;
        }

        try
        {
            sandbox.Status = SandboxStatus.Starting;
            
            await _dockerClient.Containers.StartContainerAsync(
                sandbox.ContainerId,
                new ContainerStartParameters(),
                cancellationToken);

            sandbox.Status = SandboxStatus.Running;
            sandbox.StartedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Started sandbox {SandboxId}", sandboxId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start sandbox {SandboxId}", sandboxId);
            sandbox.Status = SandboxStatus.Failed;
            throw;
        }
    }

    public async Task StopSandboxAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        var sandbox = await GetSandboxAsync(sandboxId, cancellationToken);
        
        if (sandbox.Status != SandboxStatus.Running)
        {
            return;
        }

        try
        {
            sandbox.Status = SandboxStatus.Stopping;
            
            await _dockerClient.Containers.StopContainerAsync(
                sandbox.ContainerId,
                new ContainerStopParameters { WaitBeforeKillSeconds = 10 },
                cancellationToken);

            sandbox.Status = SandboxStatus.Stopped;
            
            _logger.LogInformation("Stopped sandbox {SandboxId}", sandboxId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop sandbox {SandboxId}", sandboxId);
            throw;
        }
    }

    public async Task DestroySandboxAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        var sandbox = await GetSandboxAsync(sandboxId, cancellationToken);

        try
        {
            // Stop if running
            if (sandbox.Status == SandboxStatus.Running)
            {
                await StopSandboxAsync(sandboxId, cancellationToken);
            }

            // Remove container
            await _dockerClient.Containers.RemoveContainerAsync(
                sandbox.ContainerId,
                new ContainerRemoveParameters { Force = true },
                cancellationToken);

            // Clean up workspace
            if (Directory.Exists(sandbox.WorkspacePath))
            {
                Directory.Delete(sandbox.WorkspacePath, true);
            }

            _sandboxes.Remove(sandboxId);
            sandbox.Status = SandboxStatus.Destroyed;
            
            _logger.LogInformation("Destroyed sandbox {SandboxId}", sandboxId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to destroy sandbox {SandboxId}", sandboxId);
            throw;
        }
    }

    public async Task<SandboxExecutionResult> ExecuteCommandAsync(string sandboxId, string command, CancellationToken cancellationToken = default)
    {
        var sandbox = await GetSandboxAsync(sandboxId, cancellationToken);
        
        if (sandbox.Status != SandboxStatus.Running)
        {
            throw new InvalidOperationException($"Sandbox {sandboxId} is not running");
        }

        var execParams = new ContainerExecCreateParameters
        {
            Cmd = new[] { "/bin/sh", "-c", command },
            AttachStdout = true,
            AttachStderr = true,
            WorkingDir = "/workspace"
        };

        try
        {
            var execResponse = await _dockerClient.Exec.ExecCreateContainerAsync(
                sandbox.ContainerId,
                execParams,
                cancellationToken);

            using var stream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(
                execResponse.ID,
                false,
                cancellationToken);

            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();
            var startTime = DateTime.UtcNow;

            var buffer = new byte[4096];
            MultiplexedStream.ReadResult readResult;
            
            do
            {
                readResult = await stream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                if (readResult.Count > 0)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, readResult.Count);
                    if (readResult.Target == MultiplexedStream.TargetStream.StandardOut)
                        stdOut.Append(text);
                    else if (readResult.Target == MultiplexedStream.TargetStream.StandardError)
                        stdErr.Append(text);
                }
            } while (!readResult.EOF);

            var execInspect = await _dockerClient.Exec.InspectContainerExecAsync(execResponse.ID, cancellationToken);

            return new SandboxExecutionResult
            {
                ExitCode = (int)execInspect.ExitCode,
                StandardOutput = stdOut.ToString(),
                StandardError = stdErr.ToString(),
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command in sandbox {SandboxId}", sandboxId);
            throw;
        }
    }

    private async Task PullImageIfNeededAsync(string imageName, CancellationToken cancellationToken)
    {
        try
        {
            await _dockerClient.Images.InspectImageAsync(imageName, cancellationToken);
        }
        catch (DockerImageNotFoundException)
        {
            _logger.LogInformation("Pulling Docker image {ImageName}", imageName);
            
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = imageName },
                null,
                new Progress<JSONMessage>(msg =>
                {
                    if (!string.IsNullOrEmpty(msg.Status))
                        _logger.LogDebug("Pull progress: {Status}", msg.Status);
                }),
                cancellationToken);
        }
    }

    private SandboxStatus MapDockerStatus(ContainerState state)
    {
        return state.Status?.ToLower() switch
        {
            "created" => SandboxStatus.Created,
            "running" => SandboxStatus.Running,
            "paused" => SandboxStatus.Stopped,
            "restarting" => SandboxStatus.Starting,
            "removing" => SandboxStatus.Stopping,
            "exited" => SandboxStatus.Stopped,
            "dead" => SandboxStatus.Failed,
            _ => SandboxStatus.Stopped
        };
    }
}