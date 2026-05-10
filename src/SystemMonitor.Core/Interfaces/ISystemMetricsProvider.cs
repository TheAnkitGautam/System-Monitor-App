using SystemMonitor.Core.Models;

namespace SystemMonitor.Core.Interfaces;

/// <summary>
/// Abstraction over platform-specific system metric collection.
/// Implementations exist per OS (Windows, Linux, macOS) and are selected at runtime
/// by <c>MetricsProviderFactory</c>. The rest of the application never
/// references any concrete provider — only this interface.
/// </summary>
public interface ISystemMetricsProvider
{
    /// <summary>
    /// Returns the current overall CPU utilisation as a percentage (0–100).
    /// Implementations may need a short internal delay (e.g. 500 ms on Windows)
    /// to produce an accurate sample.
    /// </summary>
    Task<double> GetCpuUsageAsync();

    /// <summary>
    /// Returns total and used physical RAM in megabytes.
    /// </summary>
    Task<MemoryMetrics> GetRamUsageAsync();

    /// <summary>
    /// Returns total and used space of the primary fixed disk in megabytes.
    /// Uses <see cref="System.IO.DriveInfo"/> which is cross-platform.
    /// </summary>
    Task<DiskMetrics> GetDiskUsageAsync();
}
