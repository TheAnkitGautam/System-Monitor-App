using Microsoft.Extensions.Options;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Plugin.ThresholdAlert;

/// <summary>Configuration for the threshold-alert plugin.</summary>
public sealed class ThresholdAlertOptions
{
    public const string SectionName = "Plugins:ThresholdAlert";

    public bool   Enabled              { get; set; } = true;
    /// <summary>CPU % above which an alert fires. 0 = disabled.</summary>
    public double CpuThreshold         { get; set; } = 80;
    /// <summary>RAM usage % above which an alert fires. 0 = disabled.</summary>
    public double RamThresholdPercent  { get; set; } = 85;
    /// <summary>Disk usage % above which an alert fires. 0 = disabled.</summary>
    public double DiskThresholdPercent { get; set; } = 90;
    /// <summary>Minimum seconds between repeat alerts for the same metric.</summary>
    public int    CooldownSeconds      { get; set; } = 30;
}

/// <summary>Payload raised with the <see cref="ThresholdAlertPlugin.AlertTriggered"/> event.</summary>
public sealed class AlertEventArgs : EventArgs
{
    public string   MetricName { get; init; } = string.Empty;
    public double   Value      { get; init; }
    public double   Threshold  { get; init; }
    public string   Message    { get; init; } = string.Empty;
    public DateTime TriggeredAt{ get; init; } = DateTime.Now;
}

/// <summary>
/// Plugin that monitors metric values against configured thresholds and fires
/// <see cref="AlertTriggered"/> when any threshold is exceeded.
///
/// A per-metric cooldown prevents alert storms when a metric stays above threshold
/// for multiple consecutive polling cycles.
/// </summary>
public sealed class ThresholdAlertPlugin : IMonitorPlugin
{
    private readonly ThresholdAlertOptions _options;
    private readonly Dictionary<string, DateTime> _lastAlertTimes = new();

    public string Name      => "Threshold Alert";
    public bool   IsEnabled { get; set; }

    /// <summary>
    /// Raised whenever a metric crosses its configured threshold.
    /// The WPF layer subscribes to this to show toast notifications.
    /// </summary>
    public event EventHandler<AlertEventArgs>? AlertTriggered;

    public ThresholdAlertPlugin(IOptions<ThresholdAlertOptions> options)
    {
        _options  = options.Value;
        IsEnabled = _options.Enabled;
    }

    /// <inheritdoc/>
    public Task OnMetricsCollectedAsync(SystemSnapshot snapshot)
    {
        if (!IsEnabled) return Task.CompletedTask;

        CheckThreshold("CPU",  snapshot.CpuPercent,  _options.CpuThreshold,
            $"CPU usage is {snapshot.CpuPercent:F1}% (threshold: {_options.CpuThreshold}%)");

        CheckThreshold("RAM",  snapshot.RamPercent,  _options.RamThresholdPercent,
            $"RAM usage is {snapshot.RamPercent:F1}% ({snapshot.RamUsedMb} / {snapshot.RamTotalMb} MB)");

        CheckThreshold("Disk", snapshot.DiskPercent, _options.DiskThresholdPercent,
            $"Disk usage is {snapshot.DiskPercent:F1}% ({snapshot.DiskUsedMb} / {snapshot.DiskTotalMb} MB)");

        return Task.CompletedTask;
    }

    private void CheckThreshold(string metric, double value, double threshold, string message)
    {
        if (threshold <= 0 || value < threshold) return;

        // Enforce cooldown to prevent alert storms
        if (_lastAlertTimes.TryGetValue(metric, out var last) &&
            (DateTime.Now - last).TotalSeconds < _options.CooldownSeconds)
            return;

        _lastAlertTimes[metric] = DateTime.Now;

        AlertTriggered?.Invoke(this, new AlertEventArgs
        {
            MetricName = metric,
            Value      = value,
            Threshold  = threshold,
            Message    = message
        });
    }
}
