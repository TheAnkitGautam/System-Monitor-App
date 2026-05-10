using SystemMonitor.Core.Models;

namespace SystemMonitor.Core.Interfaces;
public interface IMonitorPlugin
{
    string Name { get; }

    bool IsEnabled { get; set; }
    Task OnMetricsCollectedAsync(SystemSnapshot snapshot);
}
