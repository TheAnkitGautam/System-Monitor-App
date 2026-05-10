using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Infrastructure.Providers;
[SupportedOSPlatform("windows")]
public sealed class WindowsMetricsProvider : ISystemMetricsProvider, IDisposable
{
    private readonly PerformanceCounter _cpuCounter =
        new("Processor", "% Processor Time", "_Total");

    private bool _disposed;
    public WindowsMetricsProvider()
    {
        _ = _cpuCounter.NextValue();
    }

    public async Task<double> GetCpuUsageAsync()
    {
        await Task.Delay(500).ConfigureAwait(false);
        float raw = _cpuCounter.NextValue();
        return Math.Round(Math.Max(0, Math.Min(100, raw)), 1);
    }

    public Task<MemoryMetrics> GetRamUsageAsync()
    {
        var status = new MEMORYSTATUSEX();
        if (!GlobalMemoryStatusEx(status))
            throw new InvalidOperationException(
                $"GlobalMemoryStatusEx failed with error {Marshal.GetLastWin32Error()}");

        long totalMb = (long)(status.ullTotalPhys / 1024 / 1024);
        long usedMb = (long)((status.ullTotalPhys - status.ullAvailPhys) / 1024 / 1024);

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


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
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

    public void Dispose()
    {
        if (_disposed) return;
        _cpuCounter.Dispose();
        _disposed = true;
    }
}
