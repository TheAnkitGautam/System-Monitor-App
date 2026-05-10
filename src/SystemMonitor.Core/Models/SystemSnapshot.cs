namespace SystemMonitor.Core.Models;
public record SystemSnapshot(
    DateTime Timestamp,
    double CpuPercent,
    long RamUsedMb,
    long RamTotalMb,
    long DiskUsedMb,
    long DiskTotalMb)
{
    public double RamPercent =>
        RamTotalMb > 0 ? Math.Round((double)RamUsedMb / RamTotalMb * 100, 1) : 0;
    public double DiskPercent =>
        DiskTotalMb > 0 ? Math.Round((double)DiskUsedMb / DiskTotalMb * 100, 1) : 0;
}
