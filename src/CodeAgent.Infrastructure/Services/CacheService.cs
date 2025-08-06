using System.Collections.Concurrent;
using System.Text.Json;
using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<CacheService>? _logger;
    private readonly Timer _cleanupTimer;
    private readonly object _lockObject = new();

    public CacheService(ILogger<CacheService>? logger = null)
    {
        _logger = logger;
        
        // Start cleanup timer to remove expired entries every minute
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt == null || entry.ExpiresAt > DateTime.UtcNow)
            {
                _logger?.LogDebug("Cache hit for key: {Key}", key);
                
                // Handle different value types
                if (entry.Value is T directValue)
                {
                    return Task.FromResult<T?>(directValue);
                }
                
                // Deserialize if stored as JSON string
                if (entry.Value is string json && typeof(T) != typeof(string))
                {
                    return Task.FromResult(JsonSerializer.Deserialize<T>(json));
                }
                
                return Task.FromResult<T?>(null);
            }
            else
            {
                // Entry expired, remove it
                _cache.TryRemove(key, out _);
                _logger?.LogDebug("Cache entry expired for key: {Key}", key);
            }
        }
        
        _logger?.LogDebug("Cache miss for key: {Key}", key);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var expiresAt = expiration.HasValue 
            ? DateTime.UtcNow.Add(expiration.Value) 
            : (DateTime?)null;
        
        // Serialize objects to JSON for storage (but not strings)
        object storedValue;
        if (value is string stringValue)
        {
            storedValue = stringValue;
        }
        else
        {
            storedValue = JsonSerializer.Serialize(value);
        }
        
        var entry = new CacheEntry
        {
            Value = storedValue,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
        
        _cache.AddOrUpdate(key, entry, (k, v) => entry);
        
        _logger?.LogDebug("Cache set for key: {Key}, expires: {ExpiresAt}", key, expiresAt);
        
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }
        
        // Use lock to prevent multiple factories running for the same key
        lock (_lockObject)
        {
            // Check again inside lock
            cached = GetAsync<T>(key, cancellationToken).Result;
            if (cached != null)
            {
                return cached;
            }
            
            // Create new value
            var value = factory().Result;
            SetAsync(key, value, expiration, cancellationToken).Wait();
            return value;
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryRemove(key, out _))
        {
            _logger?.LogDebug("Cache entry removed for key: {Key}", key);
        }
        
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger?.LogInformation("Cache cleared. Removed {Count} entries", count);
        
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt == null || entry.ExpiresAt > DateTime.UtcNow)
            {
                return Task.FromResult(true);
            }
            
            // Entry expired, remove it
            _cache.TryRemove(key, out _);
        }
        
        return Task.FromResult(false);
    }

    public Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default)
    {
        var keys = _cache.Keys.AsEnumerable();
        
        if (pattern != "*")
        {
            // Simple pattern matching (supports * wildcard)
            var regex = new System.Text.RegularExpressions.Regex(
                "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$");
            
            keys = keys.Where(k => regex.IsMatch(k));
        }
        
        return Task.FromResult(keys);
    }

    private void CleanupExpiredEntries(object? state)
    {
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt <= DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
        
        if (expiredKeys.Any())
        {
            _logger?.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    private class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

// File-based cache for persistent caching
public class FileCacheService : ICacheService
{
    private readonly string _cacheDirectory;
    private readonly ILogger<FileCacheService>? _logger;
    private readonly object _lockObject = new();

    public FileCacheService(string? cacheDirectory = null, ILogger<FileCacheService>? logger = null)
    {
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodeAgent",
            "Cache");
        
        _logger = logger;
        
        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var filePath = GetCacheFilePath(key);
        
        if (!File.Exists(filePath))
        {
            _logger?.LogDebug("File cache miss for key: {Key}", key);
            return null;
        }
        
        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var cacheData = JsonSerializer.Deserialize<FileCacheEntry>(content);
            
            if (cacheData != null)
            {
                if (cacheData.ExpiresAt == null || cacheData.ExpiresAt > DateTime.UtcNow)
                {
                    _logger?.LogDebug("File cache hit for key: {Key}", key);
                    return JsonSerializer.Deserialize<T>(cacheData.Value);
                }
                else
                {
                    // Entry expired, delete it
                    File.Delete(filePath);
                    _logger?.LogDebug("File cache entry expired for key: {Key}", key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reading cache file for key: {Key}", key);
        }
        
        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var filePath = GetCacheFilePath(key);
        var directory = Path.GetDirectoryName(filePath);
        
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        var cacheData = new FileCacheEntry
        {
            Value = JsonSerializer.Serialize(value),
            ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null,
            CreatedAt = DateTime.UtcNow
        };
        
        var json = JsonSerializer.Serialize(cacheData);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        
        _logger?.LogDebug("File cache set for key: {Key}, expires: {ExpiresAt}", key, cacheData.ExpiresAt);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }
        
        // Use lock to prevent multiple factories running for the same key
        lock (_lockObject)
        {
            // Check again inside lock
            cached = GetAsync<T>(key, cancellationToken).Result;
            if (cached != null)
            {
                return cached;
            }
            
            // Create new value
            var value = factory().Result;
            SetAsync(key, value, expiration, cancellationToken).Wait();
            return value;
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetCacheFilePath(key);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger?.LogDebug("File cache entry removed for key: {Key}", key);
        }
        
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(_cacheDirectory))
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.cache", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                File.Delete(file);
            }
            
            _logger?.LogInformation("File cache cleared. Removed {Count} entries", files.Length);
        }
        
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetCacheFilePath(key);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_cacheDirectory))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }
        
        var files = Directory.GetFiles(_cacheDirectory, "*.cache", SearchOption.AllDirectories);
        var keys = files.Select(f => Path.GetFileNameWithoutExtension(f));
        
        if (pattern != "*")
        {
            var regex = new System.Text.RegularExpressions.Regex(
                "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$");
            
            keys = keys.Where(k => regex.IsMatch(k));
        }
        
        return Task.FromResult(keys);
    }

    private string GetCacheFilePath(string key)
    {
        // Sanitize key for file system
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
        
        // Use subdirectories for organization (first 2 chars of hash)
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(key));
        var hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower();
        var subDir = Path.Combine(hashStr.Substring(0, 2), hashStr.Substring(2, 2));
        
        return Path.Combine(_cacheDirectory, subDir, $"{safeKey}.cache");
    }

    private class FileCacheEntry
    {
        public string Value { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}