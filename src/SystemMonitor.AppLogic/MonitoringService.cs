using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SystemMonitor.AppLogic.Configuration;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.AppLogic;

/// <summary>
/// Core orchestrator that periodically collects system metrics and dispatches them
/// to all registered plugins and the UI via the <see cref="SnapshotReady"/> event.
///
/// <para>
/// Uses <see cref="PeriodicTimer"/> (.NET 6+) which is more efficient than
/// System.Threading.Timer for async loops — it naturally back-pressures instead
/// of queuing overlapping ticks.
/// </para>
///
/// <para>
/// Plugin invocations run concurrently via <c>Task.WhenAll</c>. Each plugin is
/// wrapped in <see cref="SafeInvokePluginAsync"/> so that a single failing plugin
/// never stops the loop or affects other plugins.
/// </para>
/// </summary>
public sealed class MonitoringService : IMonitoringService, IDisposable
{
    private readonly ISystemMetricsProvider      _provider;
    private readonly IEnumerable<IMonitorPlugin> _plugins;
    private readonly IOptions<MonitoringOptions> _options;
    private readonly ILogger<MonitoringService>  _logger;

    private PeriodicTimer? _timer;
    private Task?          _loopTask;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    // ── Public events ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public event EventHandler<SystemSnapshot>?      SnapshotReady;

    /// <inheritdoc/>
    public event EventHandler<PluginErrorEventArgs>? PluginError;

    /// <inheritdoc/>
    public bool IsRunning => _loopTask is { IsCompleted: false };

    // ── Constructor ──────────────────────────────────────────────────────────

    public MonitoringService(
        ISystemMetricsProvider      provider,
        IEnumerable<IMonitorPlugin> plugins,
        IOptions<MonitoringOptions> options,
        ILogger<MonitoringService>  logger)
    {
        _provider = provider;
        _plugins  = plugins;
        _options  = options;
        _logger   = logger;
    }

    // ── IMonitoringService ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("MonitoringService.StartAsync called while already running.");
            return Task.CompletedTask;
        }

        int interval = Math.Max(1, _options.Value.IntervalSeconds);
        _logger.LogInformation("Starting monitoring loop (interval: {Interval}s).", interval);

        _cts   = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
        _loopTask = RunLoopAsync(_cts.Token);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning) return;

        _logger.LogInformation("Stopping monitoring loop...");
        _cts?.Cancel();

        if (_loopTask is not null)
        {
            try { await _loopTask.WaitAsync(cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { /* expected */ }
        }

        _logger.LogInformation("Monitoring loop stopped.");
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                SystemSnapshot snapshot;
                try
                {
                    snapshot = await CollectSnapshotAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to collect metrics snapshot.");
                    continue; // skip this tick; don't crash the loop
                }

                _logger.LogDebug("{Snapshot}", snapshot);

                // Notify UI subscribers (they must marshal to the UI thread themselves)
                SnapshotReady?.Invoke(this, snapshot);

                // Dispatch to all enabled plugins concurrently
                var enabledPlugins = _plugins.Where(p => p.IsEnabled).ToList();
                if (enabledPlugins.Count > 0)
                {
                    await Task.WhenAll(
                        enabledPlugins.Select(p => SafeInvokePluginAsync(p, snapshot))
                    ).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation — swallow silently
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Monitoring loop crashed unexpectedly.");
            throw;
        }
    }

    private async Task SafeInvokePluginAsync(IMonitorPlugin plugin, SystemSnapshot snapshot)
    {
        try
        {
            await plugin.OnMetricsCollectedAsync(snapshot).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin '{Plugin}' threw an unhandled exception.", plugin.Name);
            PluginError?.Invoke(this, new PluginErrorEventArgs
            {
                PluginName = plugin.Name,
                Exception  = ex
            });
            // Do NOT rethrow — one bad plugin must not stop others.
        }
    }

    private async Task<SystemSnapshot> CollectSnapshotAsync()
    {
        // Run all three collections concurrently to minimise total latency.
        // Note: GetCpuUsageAsync has an internal 500ms delay on Windows.
        var cpuTask  = _provider.GetCpuUsageAsync();
        var ramTask  = _provider.GetRamUsageAsync();
        var diskTask = _provider.GetDiskUsageAsync();

        await Task.WhenAll(cpuTask, ramTask, diskTask).ConfigureAwait(false);

        return new SystemSnapshot(
            Timestamp:   DateTime.Now,
            CpuPercent:  cpuTask.Result,
            RamUsedMb:   ramTask.Result.UsedMb,
            RamTotalMb:  ramTask.Result.TotalMb,
            DiskUsedMb:  diskTask.Result.UsedMb,
            DiskTotalMb: diskTask.Result.TotalMb);
    }

    // ── Disposal ─────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _cts?.Cancel();
        _cts?.Dispose();
        _timer?.Dispose();
        if (_provider is IDisposable d) d.Dispose();
        _disposed = true;
    }
}
