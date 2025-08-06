using CodeAgent.Domain.Models;

namespace CodeAgent.Domain.Interfaces;

public interface IProfileService
{
    Task<ConfigurationProfile> CreateProfileAsync(string name, string description, Dictionary<string, object> settings, CancellationToken cancellationToken = default);
    Task<ConfigurationProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConfigurationProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);
    Task<ConfigurationProfile> UpdateProfileAsync(string profileId, Dictionary<string, object> settings, CancellationToken cancellationToken = default);
    Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default);
    Task<ConfigurationProfile?> GetActiveProfileAsync(CancellationToken cancellationToken = default);
    Task<bool> SetActiveProfileAsync(string profileId, CancellationToken cancellationToken = default);
    Task<ConfigurationProfile> CloneProfileAsync(string sourceProfileId, string newName, CancellationToken cancellationToken = default);
    Task<ConfigurationProfile> SetDefaultProfileAsync(string profileId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> ExportProfileAsync(string profileId, CancellationToken cancellationToken = default);
    Task<ConfigurationProfile> ImportProfileAsync(Dictionary<string, object> profileData, CancellationToken cancellationToken = default);
}