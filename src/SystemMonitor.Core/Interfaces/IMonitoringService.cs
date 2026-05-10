using SystemMonitor.Core.Models;

namespace SystemMonitor.Core.Interfaces;

/// <summary>
/// Orchestrates periodic metric collection and plugin dispatch.
/// Implemented by <c>MonitoringService</c> in the Application layer.
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// Fired on the background thread after each successful poll cycle.
    /// Subscribers (e.g. ViewModels) must marshal updates to the UI thread.
    /// </summary>
    event EventHandler<SystemSnapshot> SnapshotReady;

    /// <summary>
    /// Fired when any plugin raises an error. Carries the plugin name and exception.
    /// </summary>
    event EventHandler<PluginErrorEventArgs> PluginError;

    /// <summary>Starts the periodic monitoring loop.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the loop gracefully, completing the current tick first.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Whether the monitoring loop is currently running.</summary>
    bool IsRunning { get; }
}

/// <summary>Event data for a plugin-level error.</summary>
public sealed class PluginErrorEventArgs : EventArgs
{
    public string    PluginName { get; init; } = string.Empty;
    public Exception Exception  { get; init; } = null!;
}
