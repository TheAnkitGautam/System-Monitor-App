using System.Runtime.Versioning;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Infrastructure.Providers;

/// <summary>
/// Linux implementation of <see cref="ISystemMetricsProvider"/>.
///
/// CPU and RAM are documented stubs — the primary development platform for this
/// submission is Windows. Disk is fully implemented via <see cref="DriveInfo"/>
/// which works cross-platform.
///
/// <para><b>To implement CPU on Linux:</b><br/>
/// Read <c>/proc/stat</c> twice with a short delay (~200 ms) and calculate the delta
/// of idle vs total jiffies:
/// <code>
///   cpu_percent = 1.0 - (delta_idle / delta_total)
/// </code>
/// </para>
///
/// <para><b>To implement RAM on Linux:</b><br/>
/// Parse <c>/proc/meminfo</c> for the <c>MemTotal</c> and <c>MemAvailable</c> lines.
/// <c>used = MemTotal - MemAvailable</c>.
/// </para>
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxMetricsProvider : ISystemMetricsProvider
{
    /// <inheritdoc/>
    /// <exception cref="NotImplementedException">
    /// Linux CPU monitoring is not yet implemented.
    /// Implement via /proc/stat delta calculation (see class-level docs).
    /// </exception>
    public Task<double> GetCpuUsageAsync() =>
        throw new NotImplementedException(
            "Linux CPU monitoring not yet implemented. " +
            "Implement via /proc/stat two-sample delta. See LinuxMetricsProvider XML docs.");

    /// <inheritdoc/>
    /// <exception cref="NotImplementedException">
    /// Linux RAM monitoring is not yet implemented.
    /// Implement via /proc/meminfo parsing (see class-level docs).
    /// </exception>
    public Task<MemoryMetrics> GetRamUsageAsync() =>
        throw new NotImplementedException(
            "Linux RAM monitoring not yet implemented. " +
            "Implement via /proc/meminfo MemTotal / MemAvailable. See LinuxMetricsProvider XML docs.");

    /// <inheritdoc/>
    /// <remarks>
    /// DriveInfo works on Linux — selects the root mount point ("/").
    /// </remarks>
    public Task<DiskMetrics> GetDiskUsageAsync()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed);

        long totalBytes = drives.Sum(d => d.TotalSize);

        long usedBytes = drives.Sum(
            d => d.TotalSize - d.AvailableFreeSpace);

        long totalMb = totalBytes / 1024 / 1024;

        long usedMb = usedBytes / 1024 / 1024;

        return Task.FromResult(
            new DiskMetrics(
                UsedMb: usedMb,
                TotalMb: totalMb));
    }
}
