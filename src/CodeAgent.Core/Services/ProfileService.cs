using System.Text.Json;
using CodeAgent.Domain.Interfaces;
using CodeAgent.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Core.Services;

public class ProfileService : IProfileService
{
    private readonly string _profilesDirectory;
    private readonly ILogger<ProfileService> _logger;
    private readonly Dictionary<string, ConfigurationProfile> _profiles = new();
    private string? _activeProfileId;

    public ProfileService(string? profilesDirectory = null, ILogger<ProfileService> logger = null!)
    {
        _profilesDirectory = profilesDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodeAgent",
            "Profiles");
        
        _logger = logger;
        
        Directory.CreateDirectory(_profilesDirectory);
        LoadProfiles();
    }

    public async Task<ConfigurationProfile> CreateProfileAsync(string name, string description, Dictionary<string, object> settings, CancellationToken cancellationToken = default)
    {
        var profile = new ConfigurationProfile
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            Settings = settings,
            CreatedAt = DateTime.UtcNow
        };

        _profiles[profile.Id] = profile;
        await SaveProfileAsync(profile, cancellationToken);
        
        _logger.LogInformation("Created configuration profile: {Name}", name);
        
        return profile;
    }

    public Task<ConfigurationProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(profileId, out var profile);
        return Task.FromResult(profile);
    }

    public Task<IEnumerable<ConfigurationProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_profiles.Values.AsEnumerable());
    }

    public async Task<ConfigurationProfile> UpdateProfileAsync(string profileId, Dictionary<string, object> settings, CancellationToken cancellationToken = default)
    {
        if (!_profiles.TryGetValue(profileId, out var profile))
        {
            throw new KeyNotFoundException($"Profile {profileId} not found");
        }

        profile.Settings = settings;
        profile.ModifiedAt = DateTime.UtcNow;
        
        await SaveProfileAsync(profile, cancellationToken);
        
        _logger.LogInformation("Updated configuration profile: {Name}", profile.Name);
        
        return profile;
    }

    public Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (!_profiles.Remove(profileId, out var profile))
        {
            return Task.FromResult(false);
        }

        var filePath = GetProfileFilePath(profileId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        if (_activeProfileId == profileId)
        {
            _activeProfileId = null;
        }

        _logger.LogInformation("Deleted configuration profile: {Name}", profile.Name);
        
        return Task.FromResult(true);
    }

    public Task<ConfigurationProfile?> GetActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        if (_activeProfileId == null)
        {
            // Try to find default profile
            var defaultProfile = _profiles.Values.FirstOrDefault(p => p.IsDefault);
            if (defaultProfile != null)
            {
                _activeProfileId = defaultProfile.Id;
            }
        }

        if (_activeProfileId != null && _profiles.TryGetValue(_activeProfileId, out var profile))
        {
            return Task.FromResult<ConfigurationProfile?>(profile);
        }

        return Task.FromResult<ConfigurationProfile?>(null);
    }

    public async Task<bool> SetActiveProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (!_profiles.ContainsKey(profileId))
        {
            return false;
        }

        _activeProfileId = profileId;
        
        // Save active profile preference
        var preferencePath = Path.Combine(_profilesDirectory, ".active");
        await File.WriteAllTextAsync(preferencePath, profileId, cancellationToken);
        
        _logger.LogInformation("Set active profile: {ProfileId}", profileId);
        
        return true;
    }

    public async Task<ConfigurationProfile> CloneProfileAsync(string sourceProfileId, string newName, CancellationToken cancellationToken = default)
    {
        if (!_profiles.TryGetValue(sourceProfileId, out var sourceProfile))
        {
            throw new KeyNotFoundException($"Source profile {sourceProfileId} not found");
        }

        var clonedProfile = new ConfigurationProfile
        {
            Id = Guid.NewGuid().ToString(),
            Name = newName,
            Description = $"Cloned from {sourceProfile.Name}",
            Settings = new Dictionary<string, object>(sourceProfile.Settings),
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _profiles[clonedProfile.Id] = clonedProfile;
        await SaveProfileAsync(clonedProfile, cancellationToken);
        
        _logger.LogInformation("Cloned profile {Source} to {New}", sourceProfile.Name, newName);
        
        return clonedProfile;
    }

    public async Task<ConfigurationProfile> SetDefaultProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (!_profiles.TryGetValue(profileId, out var profile))
        {
            throw new KeyNotFoundException($"Profile {profileId} not found");
        }

        // Clear existing default
        foreach (var p in _profiles.Values)
        {
            if (p.IsDefault)
            {
                p.IsDefault = false;
                await SaveProfileAsync(p, cancellationToken);
            }
        }

        // Set new default
        profile.IsDefault = true;
        profile.ModifiedAt = DateTime.UtcNow;
        await SaveProfileAsync(profile, cancellationToken);
        
        _logger.LogInformation("Set default profile: {Name}", profile.Name);
        
        return profile;
    }

    public Task<Dictionary<string, object>> ExportProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (!_profiles.TryGetValue(profileId, out var profile))
        {
            throw new KeyNotFoundException($"Profile {profileId} not found");
        }

        return Task.FromResult(new Dictionary<string, object>
        {
            ["id"] = profile.Id,
            ["name"] = profile.Name,
            ["description"] = profile.Description,
            ["settings"] = profile.Settings,
            ["isDefault"] = profile.IsDefault,
            ["createdAt"] = profile.CreatedAt,
            ["modifiedAt"] = profile.ModifiedAt ?? DateTime.MinValue
        });
    }

    public async Task<ConfigurationProfile> ImportProfileAsync(Dictionary<string, object> profileData, CancellationToken cancellationToken = default)
    {
        var profile = new ConfigurationProfile
        {
            Id = Guid.NewGuid().ToString(), // Generate new ID
            Name = profileData.GetValueOrDefault("name")?.ToString() ?? "Imported Profile",
            Description = profileData.GetValueOrDefault("description")?.ToString() ?? "",
            Settings = profileData.GetValueOrDefault("settings") as Dictionary<string, object> ?? new(),
            IsDefault = false, // Never import as default
            CreatedAt = DateTime.UtcNow
        };

        _profiles[profile.Id] = profile;
        await SaveProfileAsync(profile, cancellationToken);
        
        _logger.LogInformation("Imported profile: {Name}", profile.Name);
        
        return profile;
    }

    private void LoadProfiles()
    {
        if (!Directory.Exists(_profilesDirectory))
            return;

        var profileFiles = Directory.GetFiles(_profilesDirectory, "*.json");
        
        foreach (var file in profileFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<ConfigurationProfile>(json);
                if (profile != null)
                {
                    _profiles[profile.Id] = profile;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load profile from {File}", file);
            }
        }

        // Load active profile preference
        var activeFile = Path.Combine(_profilesDirectory, ".active");
        if (File.Exists(activeFile))
        {
            _activeProfileId = File.ReadAllText(activeFile).Trim();
        }

        // Create default profile if none exist
        if (!_profiles.Any())
        {
            CreateDefaultProfile();
        }
    }

    private void CreateDefaultProfile()
    {
        var defaultProfile = new ConfigurationProfile
        {
            Id = "default",
            Name = "Default",
            Description = "Default configuration profile",
            Settings = new Dictionary<string, object>
            {
                ["provider"] = "OpenAI",
                ["model"] = "gpt-4",
                ["temperature"] = 0.7,
                ["maxTokens"] = 2000,
                ["autoSave"] = true,
                ["theme"] = "dark"
            },
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };

        _profiles[defaultProfile.Id] = defaultProfile;
        SaveProfileAsync(defaultProfile).Wait();
    }

    private async Task SaveProfileAsync(ConfigurationProfile profile, CancellationToken cancellationToken = default)
    {
        var filePath = GetProfileFilePath(profile.Id);
        var directory = Path.GetDirectoryName(filePath);
        
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private string GetProfileFilePath(string profileId)
    {
        var safeId = string.Join("_", profileId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_profilesDirectory, $"{safeId}.json");
    }
}