using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models.Security;
using CodeAgent.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Infrastructure.Tests;

public class DlpServiceTests
{
    private readonly DlpService _dlpService;
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<IAuditService> _auditMock;
    private readonly Mock<ILogger<DlpService>> _loggerMock;

    public DlpServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _auditMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<DlpService>>();
        _dlpService = new DlpService(_fileSystemMock.Object, _auditMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ScanContentAsync_WithCreditCard_DetectsIt()
    {
        // Arrange
        var content = "My credit card is 4111-1111-1111-1111";

        // Act
        var result = await _dlpService.ScanContentAsync(content);

        // Assert
        result.Should().NotBeNull();
        result.HasSensitiveData.Should().BeTrue();
        result.Findings.Should().ContainSingle(f => f.Type == "Credit Card");
        result.Findings.First().Sensitivity.Should().Be(SensitivityLevel.Critical);
    }

    [Fact]
    public async Task ScanContentAsync_WithSSN_DetectsIt()
    {
        // Arrange
        var content = "SSN: 123-45-6789";

        // Act
        var result = await _dlpService.ScanContentAsync(content);

        // Assert
        result.HasSensitiveData.Should().BeTrue();
        result.Findings.Should().ContainSingle(f => f.Type == "SSN");
        result.Findings.First().Sensitivity.Should().Be(SensitivityLevel.Critical);
    }

    [Fact]
    public async Task ScanContentAsync_WithEmail_DetectsIt()
    {
        // Arrange
        var content = "Contact me at user@example.com";

        // Act
        var result = await _dlpService.ScanContentAsync(content);

        // Assert
        result.HasSensitiveData.Should().BeTrue();
        result.Findings.Should().ContainSingle(f => f.Type == "Email");
        result.Findings.First().Value.Should().Be("user@example.com");
    }

    [Fact]
    public async Task ScanContentAsync_WithApiKey_DetectsIt()
    {
        // Arrange
        var content = "API Key: ghp_1234567890abcdef1234567890abcdef1234";

        // Act
        var result = await _dlpService.ScanContentAsync(content);

        // Assert
        result.HasSensitiveData.Should().BeTrue();
        result.Findings.Should().Contain(f => f.Type == "GitHub Token");
    }

    [Fact]
    public async Task ScanContentAsync_WithNoSensitiveData_ReturnsEmpty()
    {
        // Arrange
        var content = "This is just regular text with no sensitive information.";

        // Act
        var result = await _dlpService.ScanContentAsync(content);

        // Assert
        result.HasSensitiveData.Should().BeFalse();
        result.Findings.Should().BeEmpty();
    }

    [Fact]
    public async Task RedactSensitiveDataAsync_WithFullLevel_RedactsCompletely()
    {
        // Arrange
        var content = "SSN: 123-45-6789";

        // Act
        var redacted = await _dlpService.RedactSensitiveDataAsync(content, RedactionLevel.Full);

        // Assert
        redacted.Should().Be("SSN: ***********");
    }

    [Fact]
    public async Task RedactSensitiveDataAsync_WithPartialLevel_RedactsPartially()
    {
        // Arrange
        var content = "Card: 4111111111111111";

        // Act
        var redacted = await _dlpService.RedactSensitiveDataAsync(content, RedactionLevel.Partial);

        // Assert
        // The credit card number should be partially redacted
        redacted.Should().NotBe(content);  // It should be different from original
        redacted.Should().Contain("*");     // It should contain masking characters
        redacted.Should().Contain("Card:");  // The prefix should remain
    }

    [Fact]
    public async Task RedactSensitiveDataAsync_WithSmartLevel_RedactsSmart()
    {
        // Arrange
        var content = "Email: john.doe@example.com";

        // Act
        var redacted = await _dlpService.RedactSensitiveDataAsync(content, RedactionLevel.Smart);

        // Assert
        redacted.Should().Contain("joh***@example.com");
    }

    [Fact]
    public async Task ClassifyDataAsync_WithPII_ReturnsRestrictedClassification()
    {
        // Arrange
        var content = "SSN: 123-45-6789";

        // Act
        var classification = await _dlpService.ClassifyDataAsync(content);

        // Assert
        classification.Should().NotBeNull();
        classification.Level.Should().Be(ClassificationLevel.Restricted);
        classification.Categories.Should().Contain("PII");
        classification.RequiresEncryption.Should().BeTrue();
    }

    [Fact]
    public async Task ClassifyDataAsync_WithSecrets_ReturnsTopSecretClassification()
    {
        // Arrange
        var content = "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqh...";

        // Act
        var classification = await _dlpService.ClassifyDataAsync(content);

        // Assert
        classification.Level.Should().Be(ClassificationLevel.TopSecret);
        classification.Categories.Should().Contain("Secrets");
        classification.RequiresEncryption.Should().BeTrue();
        classification.RequiresApproval.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePolicyAsync_CreatesPolicy()
    {
        // Arrange
        var policy = new DlpPolicy
        {
            Id = "test-policy",
            Name = "Test Policy",
            Description = "Test DLP Policy",
            Action = PolicyAction.Warn
        };

        // Act
        var createdPolicy = await _dlpService.CreatePolicyAsync(policy);

        // Assert
        createdPolicy.Should().NotBeNull();
        createdPolicy.Id.Should().Be(policy.Id);
    }

    [Fact]
    public async Task ValidateAgainstPolicyAsync_WithCleanContent_ReturnsTrue()
    {
        // Arrange
        var content = "This is clean content";
        var policy = new DlpPolicy
        {
            Id = "test-policy",
            Action = PolicyAction.Block,
            Rules = new List<DlpRule>()
        };
        await _dlpService.CreatePolicyAsync(policy);

        // Act
        var isValid = await _dlpService.ValidateAgainstPolicyAsync(content, policy.Id);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ScanFileAsync_WithSensitiveContent_DetectsIt()
    {
        // Arrange
        var filePath = "/test/file.txt";
        var content = "API Key: sk-1234567890abcdef";
        
        _fileSystemMock.Setup(fs => fs.FileExistsAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(fs => fs.ReadFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _dlpService.ScanFileAsync(filePath);

        // Assert
        result.HasSensitiveData.Should().BeTrue();
        result.Findings.Should().NotBeEmpty();
        result.Findings.First().FilePath.Should().Be(filePath);
    }

    [Fact]
    public async Task ScanDirectoryAsync_ScansAllFiles()
    {
        // Arrange
        var directoryPath = "/test/dir";
        var files = new[] { "/test/dir/file1.txt", "/test/dir/file2.txt" };
        
        _fileSystemMock.Setup(fs => fs.GetFilesAsync(directoryPath, "*", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);
        
        foreach (var file in files)
        {
            _fileSystemMock.Setup(fs => fs.FileExistsAsync(file, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _fileSystemMock.Setup(fs => fs.ReadFileAsync(file, It.IsAny<CancellationToken>()))
                .ReturnsAsync("Regular content");
        }

        // Act
        var result = await _dlpService.ScanDirectoryAsync(directoryPath);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().Contain($"Scanned {files.Length} files");
    }

    [Fact]
    public async Task GenerateDlpReportAsync_GeneratesReport()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        // Act
        var report = await _dlpService.GenerateDlpReportAsync(from, to);

        // Assert
        report.Should().NotBeNull();
        report.PeriodStart.Should().Be(from);
        report.PeriodEnd.Should().Be(to);
        report.Statistics.Should().ContainKey("AverageIncidentsPerDay");
    }
}