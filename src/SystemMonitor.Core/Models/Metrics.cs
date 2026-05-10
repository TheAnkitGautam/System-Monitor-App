namespace SystemMonitor.Core.Models;

public record MemoryMetrics(long UsedMb, long TotalMb);
public record DiskMetrics(long UsedMb, long TotalMb);
