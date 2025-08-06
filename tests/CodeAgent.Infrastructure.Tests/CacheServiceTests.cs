using CodeAgent.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeAgent.Infrastructure.Tests;

public class CacheServiceTests
{
    private readonly CacheService _cacheService;
    private readonly Mock<ILogger<CacheService>> _loggerMock;

    public CacheServiceTests()
    {
        _loggerMock = new Mock<ILogger<CacheService>>();
        _cacheService = new CacheService(_loggerMock.Object);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _cacheService.GetAsync<string>("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_AndGetAsync_StoresAndRetrievesValue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ExpiresAfterTimeout()
    {
        // Arrange
        const string key = "expiring-key";
        const string value = "expiring-value";
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        var immediateResult = await _cacheService.GetAsync<string>(key);
        
        await Task.Delay(150); // Wait for expiration
        var expiredResult = await _cacheService.GetAsync<string>(key);

        // Assert
        immediateResult.Should().Be(value);
        expiredResult.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCached_ReturnsCachedValue()
    {
        // Arrange
        const string key = "cached-key";
        const string cachedValue = "cached-value";
        await _cacheService.SetAsync(key, cachedValue);
        
        var factoryCalled = false;
        Func<Task<string>> factory = () =>
        {
            factoryCalled = true;
            return Task.FromResult("new-value");
        };

        // Act
        var result = await _cacheService.GetOrCreateAsync(key, factory);

        // Assert
        result.Should().Be(cachedValue);
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenNotCached_CallsFactoryAndCaches()
    {
        // Arrange
        const string key = "new-key";
        const string factoryValue = "factory-value";
        
        Func<Task<string>> factory = () => Task.FromResult(factoryValue);

        // Act
        var result = await _cacheService.GetOrCreateAsync(key, factory);
        var cachedResult = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(factoryValue);
        cachedResult.Should().Be(factoryValue);
    }

    [Fact]
    public async Task RemoveAsync_RemovesItemFromCache()
    {
        // Arrange
        const string key = "remove-key";
        const string value = "remove-value";
        await _cacheService.SetAsync(key, value);

        // Act
        await _cacheService.RemoveAsync(key);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClearAsync_RemovesAllItems()
    {
        // Arrange
        await _cacheService.SetAsync("key1", "value1");
        await _cacheService.SetAsync("key2", "value2");
        await _cacheService.SetAsync("key3", "value3");

        // Act
        await _cacheService.ClearAsync();
        var result1 = await _cacheService.GetAsync<string>("key1");
        var result2 = await _cacheService.GetAsync<string>("key2");
        var result3 = await _cacheService.GetAsync<string>("key3");

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        const string key = "exists-key";
        await _cacheService.SetAsync(key, "value");

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Act
        var exists = await _cacheService.ExistsAsync("nonexistent");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetKeysAsync_WithPattern_ReturnsMatchingKeys()
    {
        // Arrange
        await _cacheService.SetAsync("test-1", "value1");
        await _cacheService.SetAsync("test-2", "value2");
        await _cacheService.SetAsync("other-1", "value3");

        // Act
        var testKeys = await _cacheService.GetKeysAsync("test-*");
        var allKeys = await _cacheService.GetKeysAsync("*");

        // Assert
        testKeys.Should().HaveCount(2);
        testKeys.Should().Contain("test-1");
        testKeys.Should().Contain("test-2");
        testKeys.Should().NotContain("other-1");
        allKeys.Should().HaveCount(3);
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_SerializesAndDeserializes()
    {
        // Arrange
        const string key = "complex-key";
        var complexObject = new TestObject
        {
            Id = 1,
            Name = "Test",
            Tags = new List<string> { "tag1", "tag2" }
        };

        // Act
        await _cacheService.SetAsync(key, complexObject);
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
        result.Tags.Should().BeEquivalentTo(new[] { "tag1", "tag2" });
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }
}

public class FileCacheServiceTests : IDisposable
{
    private readonly FileCacheService _cacheService;
    private readonly string _testCacheDir;
    private readonly Mock<ILogger<FileCacheService>> _loggerMock;

    public FileCacheServiceTests()
    {
        _testCacheDir = Path.Combine(Path.GetTempPath(), $"cache-test-{Guid.NewGuid()}");
        _loggerMock = new Mock<ILogger<FileCacheService>>();
        _cacheService = new FileCacheService(_testCacheDir, _loggerMock.Object);
    }

    [Fact]
    public async Task SetAsync_AndGetAsync_PersistsToDisk()
    {
        // Arrange
        const string key = "persist-key";
        const string value = "persist-value";

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
        Directory.GetFiles(_testCacheDir, "*.cache", SearchOption.AllDirectories)
            .Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAsync_AfterRestart_StillReturnsValue()
    {
        // Arrange
        const string key = "restart-key";
        const string value = "restart-value";
        await _cacheService.SetAsync(key, value);

        // Act - Create new instance (simulating restart)
        var newCacheService = new FileCacheService(_testCacheDir, _loggerMock.Object);
        var result = await newCacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task RemoveAsync_DeletesFileFromDisk()
    {
        // Arrange
        const string key = "delete-key";
        await _cacheService.SetAsync(key, "value");
        var filesBefore = Directory.GetFiles(_testCacheDir, "*.cache", SearchOption.AllDirectories);

        // Act
        await _cacheService.RemoveAsync(key);
        var filesAfter = Directory.GetFiles(_testCacheDir, "*.cache", SearchOption.AllDirectories);

        // Assert
        filesBefore.Should().NotBeEmpty();
        filesAfter.Should().HaveCountLessThan(filesBefore.Length);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllCacheFiles()
    {
        // Arrange
        await _cacheService.SetAsync("file1", "value1");
        await _cacheService.SetAsync("file2", "value2");

        // Act
        await _cacheService.ClearAsync();
        var files = Directory.GetFiles(_testCacheDir, "*.cache", SearchOption.AllDirectories);

        // Assert
        files.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testCacheDir))
        {
            Directory.Delete(_testCacheDir, true);
        }
    }
}