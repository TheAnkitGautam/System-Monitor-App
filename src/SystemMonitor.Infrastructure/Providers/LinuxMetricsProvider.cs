using System.Runtime.Versioning;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Infrastructure.Providers;

[SupportedOSPlatform("linux")]
public sealed class LinuxMetricsProvider : ISystemMetricsProvider
{
    public Task<double> GetCpuUsageAsync() =>
        throw new NotImplementedException(
            "Linux CPU monitoring not yet implemented.");

    public Task<MemoryMetrics> GetRamUsageAsync() =>
        throw new NotImplementedException(
            "Linux RAM monitoring not yet implemented.");

    public Task<DiskMetrics> GetDiskUsageAsync() =>
     throw new NotImplementedException(
         "Linux disk monitoring not yet implemented.");
}
