using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models.Security;
using CodeAgent.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Infrastructure.Tests;

public class SandboxServiceTests
{
    private readonly SandboxService _sandboxService;
    private readonly Mock<ILogger<SandboxService>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;

    public SandboxServiceTests()
    {
        _loggerMock = new Mock<ILogger<SandboxService>>();
        _auditServiceMock = new Mock<IAuditService>();
        _sandboxService = new SandboxService(_loggerMock.Object, _auditServiceMock.Object);
    }

    [Fact]
    public async Task CreateSandboxAsync_WithValidConfiguration_CreatesSandbox()
    {
        // Arrange
        var config = new SandboxConfiguration
        {
            Name = "Test Sandbox",
            Type = SandboxType.Process,
            ResourceLimits = new ResourceLimits
            {
                MaxMemoryBytes = 1024 * 1024 * 512, // 512MB
                MaxCpuPercent = 50
            }
        };

        // Act
        var sandbox = await _sandboxService.CreateSandboxAsync(config);

        // Assert
        sandbox.Should().NotBeNull();
        sandbox.Name.Should().Be(config.Name);
        sandbox.Type.Should().Be(config.Type);
        sandbox.Status.Should().Be(SandboxStatus.Ready);
        sandbox.ResourceLimits.MaxMemoryBytes.Should().Be(config.ResourceLimits.MaxMemoryBytes);
    }

    [Fact]
    public async Task CreateSandboxAsync_WithFileSystemType_CreatesSandbox()
    {
        // Arrange
        var config = new SandboxConfiguration
        {
            Name = "FS Sandbox",
            Type = SandboxType.FileSystem
        };

        // Act
        var sandbox = await _sandboxService.CreateSandboxAsync(config);

        // Assert
        sandbox.Should().NotBeNull();
        sandbox.Type.Should().Be(SandboxType.FileSystem);
        sandbox.Status.Should().Be(SandboxStatus.Ready);
        sandbox.Metadata.Should().ContainKey("RootPath");
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_WithValidCode_ReturnsResult()
    {
        // Arrange
        var config = new SandboxConfiguration
        {
            Name = "Exec Sandbox",
            Type = SandboxType.Process
        };
        var sandbox = await _sandboxService.CreateSandboxAsync(config);
        var code = "echo Hello";

        // Act
        var result = await _sandboxService.ExecuteInSandboxAsync(sandbox.Id, code);

        // Assert
        result.Should().NotBeNull();
        result.SandboxId.Should().Be(sandbox.Id);
        // Process execution may vary by OS, so we just check it completed
        result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_WithDangerousCommand_BlocksExecution()
    {
        // Arrange
        var config = new SandboxConfiguration
        {
            Name = "Security Sandbox",
            Type = SandboxType.Process
        };
        var sandbox = await _sandboxService.CreateSandboxAsync(config);
        var dangerousCode = "rm -rf /";

        // Act
        var result = await _sandboxService.ExecuteInSandboxAsync(sandbox.Id, dangerousCode);

        // Assert
        result.Success.Should().BeFalse();
        result.SecurityViolations.Should().NotBeEmpty();
        result.SecurityViolations.Should().Contain(v => v.Type == "DangerousCommand");
    }

    [Fact]
    public async Task DestroySandboxAsync_WithExistingSandbox_DestroysSandbox()
    {
        // Arrange
        var config = new SandboxConfiguration { Name = "Temp Sandbox", Type = SandboxType.FileSystem };
        var sandbox = await _sandboxService.CreateSandboxAsync(config);

        // Act
        var result = await _sandboxService.DestroySandboxAsync(sandbox.Id);

        // Assert
        result.Should().BeTrue();
        var status = await _sandboxService.GetSandboxStatusAsync(sandbox.Id);
        status.Should().Be(SandboxStatus.Destroyed);
    }

    [Fact]
    public async Task DestroySandboxAsync_WithNonExistentSandbox_ReturnsFalse()
    {
        // Act
        var result = await _sandboxService.DestroySandboxAsync("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetResourceUsageAsync_ReturnsUsage()
    {
        // Arrange
        var config = new SandboxConfiguration { Name = "Resource Sandbox", Type = SandboxType.Process };
        var sandbox = await _sandboxService.CreateSandboxAsync(config);

        // Act
        var usage = await _sandboxService.GetResourceUsageAsync(sandbox.Id);

        // Assert
        usage.Should().NotBeNull();
        usage.MeasuredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SetResourceLimitsAsync_UpdatesLimits()
    {
        // Arrange
        var config = new SandboxConfiguration { Name = "Limit Sandbox", Type = SandboxType.Process };
        var sandbox = await _sandboxService.CreateSandboxAsync(config);
        var newLimits = new ResourceLimits
        {
            MaxMemoryBytes = 1024 * 1024 * 1024, // 1GB
            MaxCpuPercent = 75
        };

        // Act
        var result = await _sandboxService.SetResourceLimitsAsync(sandbox.Id, newLimits);

        // Assert
        result.Should().BeTrue();
        var activeSandboxes = await _sandboxService.GetActiveSandboxesAsync();
        var updatedSandbox = activeSandboxes.FirstOrDefault(s => s.Id == sandbox.Id);
        updatedSandbox?.ResourceLimits.MaxMemoryBytes.Should().Be(newLimits.MaxMemoryBytes);
    }

    [Fact]
    public async Task GetActiveSandboxesAsync_ReturnsOnlyActiveSandboxes()
    {
        // Arrange
        var config1 = new SandboxConfiguration { Name = "Active 1", Type = SandboxType.FileSystem };
        var config2 = new SandboxConfiguration { Name = "Active 2", Type = SandboxType.FileSystem };
        
        var sandbox1 = await _sandboxService.CreateSandboxAsync(config1);
        var sandbox2 = await _sandboxService.CreateSandboxAsync(config2);
        
        // Destroy one sandbox
        await _sandboxService.DestroySandboxAsync(sandbox1.Id);

        // Act
        var activeSandboxes = await _sandboxService.GetActiveSandboxesAsync();

        // Assert
        activeSandboxes.Should().HaveCount(1);
        activeSandboxes.Should().Contain(s => s.Id == sandbox2.Id);
        activeSandboxes.Should().NotContain(s => s.Id == sandbox1.Id);
    }

    [Fact]
    public async Task IsolateSandboxNetworkAsync_SetsIsolationLevel()
    {
        // Arrange
        var config = new SandboxConfiguration { Name = "Network Sandbox", Type = SandboxType.Process };
        var sandbox = await _sandboxService.CreateSandboxAsync(config);

        // Act
        var result = await _sandboxService.IsolateSandboxNetworkAsync(sandbox.Id, NetworkIsolationLevel.Complete);

        // Assert
        result.Should().BeTrue();
        var activeSandboxes = await _sandboxService.GetActiveSandboxesAsync();
        var updatedSandbox = activeSandboxes.FirstOrDefault(s => s.Id == sandbox.Id);
        updatedSandbox?.NetworkIsolation.Should().Be(NetworkIsolationLevel.Complete);
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_WithNonExistentSandbox_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sandboxService.ExecuteInSandboxAsync("non-existent", "echo test"));
    }

    [Fact]
    public async Task CreateSandboxAsync_WithContainerType_CreatesSandbox()
    {
        // Arrange
        var config = new SandboxConfiguration
        {
            Name = "Container Sandbox",
            Type = SandboxType.Container
        };

        // Act
        var sandbox = await _sandboxService.CreateSandboxAsync(config);

        // Assert
        sandbox.Should().NotBeNull();
        sandbox.Type.Should().Be(SandboxType.Container);
        sandbox.Status.Should().Be(SandboxStatus.Ready);
    }
}