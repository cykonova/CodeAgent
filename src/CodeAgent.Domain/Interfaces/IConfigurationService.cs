namespace CodeAgent.Domain.Interfaces;

public interface IConfigurationService
{
    T GetSection<T>(string sectionName) where T : class, new();
    string? GetValue(string key);
    void SetValue(string key, string value);
    Task SaveAsync(CancellationToken cancellationToken = default);
}