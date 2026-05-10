using System.Runtime.Versioning;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Infrastructure.Providers;

/// <summary>
/// macOS implementation of <see cref="ISystemMetricsProvider"/>.
///
/// CPU and RAM are documented stubs. Disk is fully implemented.
///
/// <para><b>To implement CPU on macOS:</b><br/>
/// Use <c>sysctl -n vm.loadavg</c> or invoke <c>host_statistics</c> via
/// P/Invoke into <c>libSystem.dylib</c> to get <c>CPU_STATE_IDLE</c> ticks.
/// </para>
///
/// <para><b>To implement RAM on macOS:</b><br/>
/// Call <c>sysctl hw.memsize</c> for total physical bytes, then parse
/// <c>vm_stat</c> output for page counts: used = (active + wired) * page_size.
/// </para>
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacOsMetricsProvider : ISystemMetricsProvider
{
    /// <inheritdoc/>
    /// <exception cref="NotImplementedException">
    /// macOS CPU monitoring not yet implemented. See class-level docs.
    /// </exception>
    public Task<double> GetCpuUsageAsync() =>
        throw new NotImplementedException(
            "macOS CPU monitoring not yet implemented. " +
            "Implement via sysctl vm.loadavg or host_statistics P/Invoke.");

    /// <inheritdoc/>
    /// <exception cref="NotImplementedException">
    /// macOS RAM monitoring not yet implemented. See class-level docs.
    /// </exception>
    public Task<MemoryMetrics> GetRamUsageAsync() =>
        throw new NotImplementedException(
            "macOS RAM monitoring not yet implemented. " +
            "Implement via sysctl hw.memsize + vm_stat page parsing.");

    /// <inheritdoc/>
    public Task<DiskMetrics> GetDiskUsageAsync()
    {
        var drive = DriveInfo.GetDrives()
                             .FirstOrDefault(d => d.IsReady && d.RootDirectory.FullName == "/")
                   ?? DriveInfo.GetDrives().First(d => d.IsReady);

        long totalMb = (long)(drive.TotalSize / 1024 / 1024);
        long usedMb  = (long)((drive.TotalSize - drive.AvailableFreeSpace) / 1024 / 1024);

        return Task.FromResult(new DiskMetrics(UsedMb: usedMb, TotalMb: totalMb));
    }
}
