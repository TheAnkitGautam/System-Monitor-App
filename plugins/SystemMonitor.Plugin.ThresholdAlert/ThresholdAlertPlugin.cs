using Microsoft.Extensions.Options;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Plugin.ThresholdAlert;

// Configuration for the threshold-alert plugin
public sealed class ThresholdAlertOptions
{
    public const string SectionName = "Plugins:ThresholdAlert";

    public bool Enabled { get; set; }

    public double CpuThreshold { get; set; }

    public double RamThresholdPercent { get; set; }

    public double DiskThresholdPercent { get; set; }

    public int CooldownSeconds { get; set; }
}

public sealed class AlertEventArgs : EventArgs
{
    public string MetricName { get; init; } = string.Empty;

    public double Value { get; init; }

    public double Threshold { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime TriggeredAt { get; init; } = DateTime.Now;
}

public sealed class ThresholdAlertPlugin : IMonitorPlugin
{
    private readonly IOptionsMonitor<ThresholdAlertOptions> _options;

    private readonly Dictionary<string, DateTime> _lastAlertTimes = new();

    public string Name => "Threshold Alert";

    public bool IsEnabled { get; set; }

    public event EventHandler<AlertEventArgs>? AlertTriggered;

    public ThresholdAlertPlugin(
        IOptionsMonitor<ThresholdAlertOptions> options)
    {
        _options = options;

        // Apply initial settings
        ApplyOptions(_options.CurrentValue);

        // React to runtime config changes
        _options.OnChange(ApplyOptions);
    }

    public Task OnMetricsCollectedAsync(SystemSnapshot snapshot)
    {
        if (!IsEnabled)
            return Task.CompletedTask;

        var options = _options.CurrentValue;

        CheckThreshold(
            metric: "CPU",
            value: snapshot.CpuPercent,
            threshold: options.CpuThreshold,
            message:
                $"CPU usage is {snapshot.CpuPercent:F1}% " +
                $"(threshold: {options.CpuThreshold:F1}%)");

        CheckThreshold(
            metric: "RAM",
            value: snapshot.RamPercent,
            threshold: options.RamThresholdPercent,
            message:
                $"RAM usage is {snapshot.RamPercent:F1}% " +
                $"({snapshot.RamUsedMb} / {snapshot.RamTotalMb} MB)");

        CheckThreshold(
            metric: "Disk",
            value: snapshot.DiskPercent,
            threshold: options.DiskThresholdPercent,
            message:
                $"Disk usage is {snapshot.DiskPercent:F1}% " +
                $"({snapshot.DiskUsedMb} / {snapshot.DiskTotalMb} MB)");

        return Task.CompletedTask;
    }

    private void CheckThreshold(
        string metric,
        double value,
        double threshold,
        string message)
    {
        if (threshold <= 0 || value < threshold)
            return;

        var options = _options.CurrentValue;

        // Prevent alert spam using cooldown
        if (_lastAlertTimes.TryGetValue(metric, out var lastAlert))
        {
            var secondsSinceLastAlert =
                (DateTime.Now - lastAlert).TotalSeconds;

            if (secondsSinceLastAlert < options.CooldownSeconds)
            {
                return;
            }
        }

        _lastAlertTimes[metric] = DateTime.Now;

        AlertTriggered?.Invoke(
            this,
            new AlertEventArgs
            {
                MetricName = metric,
                Value = value,
                Threshold = threshold,
                Message = message,
                TriggeredAt = DateTime.Now
            });
    }

    private void ApplyOptions(ThresholdAlertOptions options)
    {
        IsEnabled = options.Enabled;
    }
}