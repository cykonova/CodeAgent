namespace CodeAgent.Sandbox.Services;

public interface IArtifactServer
{
    Task<ArtifactInfo> RegisterArtifactAsync(string sandboxId, ArtifactRegistration registration, CancellationToken cancellationToken = default);
    Task<ArtifactInfo> GetArtifactAsync(string artifactId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ArtifactInfo>> ListArtifactsAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task<Stream> DownloadArtifactAsync(string artifactId, CancellationToken cancellationToken = default);
    string GetPreviewUrl(string artifactId);
}