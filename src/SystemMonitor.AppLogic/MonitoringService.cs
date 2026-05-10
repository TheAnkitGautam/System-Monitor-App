using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SystemMonitor.AppLogic.Configuration;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.AppLogic;

public sealed class MonitoringService : IMonitoringService, IDisposable
{
    private readonly ISystemMetricsProvider _provider;
    private readonly IEnumerable<IMonitorPlugin> _plugins;
    private readonly IOptionsMonitor<MonitoringOptions> _options;

    // Prevents concurrent Start/Stop/Restart calls
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private PeriodicTimer? _timer;
    private Task? _loopTask;
    private CancellationTokenSource? _cts;

    private bool _disposed;

    public event EventHandler<SystemSnapshot>? SnapshotReady;

    public event EventHandler<PluginErrorEventArgs>? PluginError;

    public bool IsRunning =>
        _loopTask is { IsCompleted: false };

    public MonitoringService(
        ISystemMetricsProvider provider,
        IEnumerable<IMonitorPlugin> plugins,
        IOptionsMonitor<MonitoringOptions> options)
    {
        _provider = provider;
        _plugins = plugins;
        _options = options;
    }

    // ─────────────────────────────────────────────────────────────
    // START LOOP
    // ─────────────────────────────────────────────────────────────

    public async Task StartAsync(
        CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken);

        try
        {
            if (IsRunning)
            {
                return;
            }

            int interval = Math.Max(
                1,
                _options.CurrentValue.IntervalSeconds);

            _cts = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken);

            _timer = new PeriodicTimer(
                TimeSpan.FromSeconds(interval));

            _loopTask = RunLoopAsync(
                _timer,
                _cts.Token);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // STOP
    // ─────────────────────────────────────────────────────────────

    public async Task StopAsync(
        CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken);

        try
        {
            if (!IsRunning)
            {
                return;
            }

            _cts?.Cancel();

            if (_loopTask is not null)
            {
                try
                {
                    await _loopTask
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            }

            _timer?.Dispose();
            _cts?.Dispose();

            _timer = null;
            _cts = null;
            _loopTask = null;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // RESTART
    // ─────────────────────────────────────────────────────────────

    public async Task RestartAsync(
        CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────
    // MAIN LOOP
    // ─────────────────────────────────────────────────────────────

    private async Task RunLoopAsync(
        PeriodicTimer timer,
        CancellationToken ct)
    {
        try
        {
            while (await timer
                .WaitForNextTickAsync(ct)
                .ConfigureAwait(false))
            {
                SystemSnapshot snapshot;

                try
                {
                    snapshot = await CollectSnapshotAsync()
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    continue;
                }

                // Notify UI
                SnapshotReady?.Invoke(this, snapshot);

                // Execute plugins concurrently
                var enabledPlugins = _plugins
                    .Where(p => p.IsEnabled)
                    .ToList();

                if (enabledPlugins.Count > 0)
                {
                    await Task.WhenAll(
                        enabledPlugins.Select(
                            p => SafeInvokePluginAsync(
                                p,
                                snapshot)))
                        .ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown/restart
        }
        catch (ObjectDisposedException)
        {
            // Expected if timer disposed during shutdown
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // PLUGIN EXECUTION
    // ─────────────────────────────────────────────────────────────

    private async Task SafeInvokePluginAsync(
        IMonitorPlugin plugin,
        SystemSnapshot snapshot)
    {
        try
        {
            await plugin
                .OnMetricsCollectedAsync(snapshot)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PluginError?.Invoke(
                this,
                new PluginErrorEventArgs
                {
                    PluginName = plugin.Name,
                    Exception = ex
                });
        }
    }

    // ─────────────────────────────────────────────────────────────
    // METRICS COLLECTION
    // ─────────────────────────────────────────────────────────────

    private async Task<SystemSnapshot> CollectSnapshotAsync()
    {
        var cpuTask = _provider.GetCpuUsageAsync();
        var ramTask = _provider.GetRamUsageAsync();
        var diskTask = _provider.GetDiskUsageAsync();

        await Task.WhenAll(
            cpuTask,
            ramTask,
            diskTask)
            .ConfigureAwait(false);

        return new SystemSnapshot(
            Timestamp: DateTime.Now,

            CpuPercent: cpuTask.Result,

            RamUsedMb: ramTask.Result.UsedMb,
            RamTotalMb: ramTask.Result.TotalMb,

            DiskUsedMb: diskTask.Result.UsedMb,
            DiskTotalMb: diskTask.Result.TotalMb);
    }

    // ─────────────────────────────────────────────────────────────
    // DISPOSAL
    // ─────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cts?.Cancel();

        _timer?.Dispose();
        _cts?.Dispose();
        _stateLock.Dispose();

        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}