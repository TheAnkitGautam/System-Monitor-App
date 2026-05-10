using System.Runtime.Versioning;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Infrastructure.Providers;

[SupportedOSPlatform("macos")]
public sealed class MacOsMetricsProvider : ISystemMetricsProvider
{
    public Task<double> GetCpuUsageAsync() =>
        throw new NotImplementedException(
            "macOS CPU monitoring not yet implemented.");

    public Task<MemoryMetrics> GetRamUsageAsync() =>
        throw new NotImplementedException(
            "macOS RAM monitoring not yet implemented.");

    public Task<DiskMetrics> GetDiskUsageAsync() =>
    throw new NotImplementedException(
        "Linux disk monitoring not yet implemented.");
}
