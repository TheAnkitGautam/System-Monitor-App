namespace SystemMonitor.WPF.Models;

public sealed class AlertNotification
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string MetricName { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; } = DateTime.Now;
}