using SystemMonitor.Core.Models;

namespace SystemMonitor.Core.Interfaces;
public interface IMonitoringService
{
    event EventHandler<SystemSnapshot> SnapshotReady;
    event EventHandler<PluginErrorEventArgs> PluginError;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task RestartAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
}
public sealed class PluginErrorEventArgs : EventArgs
{
    public string PluginName { get; init; } = string.Empty;
    public Exception Exception { get; init; } = null!;
}
