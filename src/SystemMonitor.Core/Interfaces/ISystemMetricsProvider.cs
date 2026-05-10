using SystemMonitor.Core.Models;

namespace SystemMonitor.Core.Interfaces;
public interface ISystemMetricsProvider
{
    Task<double> GetCpuUsageAsync();
    Task<MemoryMetrics> GetRamUsageAsync();
    Task<DiskMetrics> GetDiskUsageAsync();
}
