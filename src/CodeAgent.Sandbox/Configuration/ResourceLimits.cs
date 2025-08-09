namespace CodeAgent.Sandbox.Configuration;

public class ResourceLimits
{
    public double CpuLimit      { get; set; } = 2.0;
    public long   MemoryLimitMB { get; set; } = 2048;
    public long   DiskSpaceMB   { get; set; } = 10240;
    public int    MaxProcesses  { get; set; } = 100;
}