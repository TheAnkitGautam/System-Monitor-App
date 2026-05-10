using SystemMonitor.Core.Models;

namespace SystemMonitor.Core.Interfaces;

/// <summary>
/// Contract that every monitoring plugin must implement.
/// Plugins are invoked concurrently by <c>MonitoringService</c> after each polling tick.
/// A plugin that throws will have its exception caught and logged; it will not
/// stop other plugins or the monitoring loop.
/// </summary>
public interface IMonitorPlugin
{
    /// <summary>Human-readable name shown in the Plugins view.</summary>
    string Name { get; }

    /// <summary>
    /// When <c>false</c> the plugin is skipped during dispatch.
    /// Can be toggled at runtime without restarting the service.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Called once per monitoring tick with the freshly collected snapshot.
    /// Must be implemented as async-safe; long-running work should use
    /// <c>ConfigureAwait(false)</c> and avoid blocking the thread pool.
    /// </summary>
    /// <param name="snapshot">The current system resource snapshot.</param>
    Task OnMetricsCollectedAsync(SystemSnapshot snapshot);
}
