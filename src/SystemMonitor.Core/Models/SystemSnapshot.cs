namespace SystemMonitor.Core.Models;

/// <summary>
/// Immutable snapshot of all monitored system resources captured at a single point in time.
/// Passed to all plugins and the UI on every polling tick.
/// </summary>
public record SystemSnapshot(
    DateTime Timestamp,
    double   CpuPercent,
    long     RamUsedMb,
    long     RamTotalMb,
    long     DiskUsedMb,
    long     DiskTotalMb)
{
    /// <summary>RAM usage expressed as a percentage (0–100).</summary>
    public double RamPercent =>
        RamTotalMb > 0 ? Math.Round((double)RamUsedMb / RamTotalMb * 100, 1) : 0;

    /// <summary>Disk usage expressed as a percentage (0–100).</summary>
    public double DiskPercent =>
        DiskTotalMb > 0 ? Math.Round((double)DiskUsedMb / DiskTotalMb * 100, 1) : 0;

    /// <summary>Human-readable summary for console / log output.</summary>
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] " +
        $"CPU: {CpuPercent,5:F1}% | " +
        $"RAM: {RamUsedMb,6} / {RamTotalMb,6} MB ({RamPercent:F1}%) | " +
        $"Disk: {DiskUsedMb,8} / {DiskTotalMb,8} MB ({DiskPercent:F1}%)";
}
