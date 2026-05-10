using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Infrastructure.Providers;

/// <summary>
/// Full Windows implementation of <see cref="ISystemMetricsProvider"/>.
/// Uses <see cref="PerformanceCounter"/> for CPU, Win32 GlobalMemoryStatusEx P/Invoke
/// for RAM, and <see cref="DriveInfo"/> for disk (cross-platform safe).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsMetricsProvider : ISystemMetricsProvider, IDisposable
{
    // PerformanceCounter for overall processor utilisation.
    // Note: the first call to NextValue() always returns 0.0 – we prime it in the constructor.
    private readonly PerformanceCounter _cpuCounter =
        new("Processor", "% Processor Time", "_Total");

    private bool _disposed;

    /// <summary>
    /// Constructs the provider and primes the CPU counter.
    /// The priming read is discarded; subsequent reads will be accurate.
    /// </summary>
    public WindowsMetricsProvider()
    {
        // Discard the always-zero first sample so real reads are immediately valid.
        _ = _cpuCounter.NextValue();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A 500 ms delay is required between successive PerformanceCounter reads to
    /// allow the OS to accumulate a valid CPU time delta. This is intentional and
    /// expected; callers should account for it in the polling interval.
    /// </remarks>
    public async Task<double> GetCpuUsageAsync()
    {
        await Task.Delay(500).ConfigureAwait(false);
        float raw = _cpuCounter.NextValue();
        return Math.Round(Math.Max(0, Math.Min(100, raw)), 1);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses the native Win32 GlobalMemoryStatusEx for byte-accurate physical
    /// memory information, which is more reliable than PerformanceCounter RAM counters.
    /// </remarks>
    public Task<MemoryMetrics> GetRamUsageAsync()
    {
        var status = new MEMORYSTATUSEX();
        if (!GlobalMemoryStatusEx(status))
            throw new InvalidOperationException(
                $"GlobalMemoryStatusEx failed with error {Marshal.GetLastWin32Error()}");

        long totalMb = (long)(status.ullTotalPhys / 1024 / 1024);
        long usedMb  = (long)((status.ullTotalPhys - status.ullAvailPhys) / 1024 / 1024);

        return Task.FromResult(new MemoryMetrics(UsedMb: usedMb, TotalMb: totalMb));
    }

    /// <inheritdoc/>
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

    // ── Win32 P/Invoke ──────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint  dwLength       = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint  dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    // ── Disposal ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _cpuCounter.Dispose();
        _disposed = true;
    }
}
