namespace SystemMonitor.AppLogic.Configuration;
public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    public int IntervalSeconds { get; set; }
}
