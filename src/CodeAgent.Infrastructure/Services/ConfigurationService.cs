using System.Text.Json;
using CodeAgent.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CodeAgent.Infrastructure.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, string> _runtimeSettings;
    private readonly string _settingsPath;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
        _runtimeSettings = new Dictionary<string, string>();
        
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userHome, ".codeagent");
        Directory.CreateDirectory(configDir);
        _settingsPath = Path.Combine(configDir, "settings.json");
        
        LoadSettings();
    }

    public T GetSection<T>(string sectionName) where T : class, new()
    {
        var section = _configuration.GetSection(sectionName);
        var result = section.Get<T>() ?? new T();
        
        // Apply runtime settings
        foreach (var setting in _runtimeSettings)
        {
            if (setting.Key.StartsWith($"{sectionName}:"))
            {
                var propertyPath = setting.Key.Substring(sectionName.Length + 1);
                ApplySettingToObject(result, propertyPath, setting.Value);
            }
        }
        
        return result;
    }

    public string? GetValue(string key)
    {
        if (_runtimeSettings.TryGetValue(key, out var value))
        {
            return value;
        }
        return _configuration[key];
    }

    public void SetValue(string key, string value)
    {
        _runtimeSettings[key] = value;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(_runtimeSettings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_settingsPath, json, cancellationToken);
    }

    private void LoadSettings()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (settings != null)
                {
                    foreach (var kvp in settings)
                    {
                        _runtimeSettings[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch
            {
                // Ignore errors loading settings
            }
        }
    }

    private void ApplySettingToObject(object obj, string propertyPath, string value)
    {
        var parts = propertyPath.Split('.');
        var current = obj;
        
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var prop = current.GetType().GetProperty(parts[i]);
            if (prop != null)
            {
                current = prop.GetValue(current) ?? Activator.CreateInstance(prop.PropertyType)!;
                prop.SetValue(current, current);
            }
        }
        
        var finalProp = current.GetType().GetProperty(parts[^1]);
        if (finalProp != null)
        {
            var convertedValue = Convert.ChangeType(value, finalProp.PropertyType);
            finalProp.SetValue(current, convertedValue);
        }
    }
}