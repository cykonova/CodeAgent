namespace CodeAgent.Sandbox.Configuration;

public class NetworkOptions
{
    public bool     AllowExternal { get; set; } = false;
    public string[] AllowedHosts  { get; set; } = { "registry.npmjs.org", "pypi.org", "nuget.org" };
    public int[]    ExposedPorts  { get; set; } = { 3000, 8080, 5000 };
}