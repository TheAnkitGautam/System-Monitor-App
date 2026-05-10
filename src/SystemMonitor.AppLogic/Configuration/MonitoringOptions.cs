namespace SystemMonitor.AppLogic.Configuration;

/// <summary>
/// Strongly-typed binding for the "Monitoring" section of appsettings.json.
/// Injected as <c>IOptions&lt;MonitoringOptions&gt;</c>.
/// </summary>
public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    /// <summary>
    /// How frequently metrics are collected, in seconds.
    /// Must be at least 1. Defaults to 5.
    /// </summary>
    public int IntervalSeconds { get; set; } = 5;
}
