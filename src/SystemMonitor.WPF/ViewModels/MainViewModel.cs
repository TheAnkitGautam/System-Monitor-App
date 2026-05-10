using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;
using SystemMonitor.Plugin.ThresholdAlert;

namespace SystemMonitor.WPF.ViewModels;

/// <summary>
/// Root ViewModel responsible for:
/// - monitoring lifecycle
/// - navigation
/// - forwarding events to child ViewModels
/// </summary>
public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IMonitoringService _service;
    private readonly ThresholdAlertPlugin _alertPlugin;

    public DashboardViewModel Dashboard { get; }

    public PluginsViewModel Plugins { get; }

    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private bool isMonitoring;

    [ObservableProperty]
    private int selectedTabIndex;

    [ObservableProperty]
    private string statusText = "Ready";

    public MainViewModel(
        IMonitoringService service,
        ThresholdAlertPlugin alertPlugin,
        DashboardViewModel dashboard,
        PluginsViewModel plugins,
        SettingsViewModel settings)
    {
        _service = service;
        _alertPlugin = alertPlugin;

        Dashboard = dashboard;
        Plugins = plugins;
        Settings = settings;

        // Subscribe to events
        _service.SnapshotReady += OnSnapshotReceived;
        _service.PluginError += OnPluginError;
        _alertPlugin.AlertTriggered += OnAlertTriggered;
    }

    // ─────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────

    [RelayCommand]
    private async Task StartMonitoringAsync()
    {
        await _service.StartAsync();

        IsMonitoring = true;
        StatusText = "Monitoring...";
    }

    [RelayCommand]
    private async Task StopMonitoringAsync()
    {
        await _service.StopAsync();

        IsMonitoring = false;
        StatusText = "Stopped";
    }

    [RelayCommand]
    private async Task ToggleMonitoringAsync()
    {
        if (IsMonitoring)
        {
            await StopMonitoringAsync();
        }
        else
        {
            await StartMonitoringAsync();
        }
    }

    [RelayCommand]
    private void Navigate(string tabIndex)
    {
        if (int.TryParse(tabIndex, out int idx))
        {
            SelectedTabIndex = idx;
        }
    }

    // ─────────────────────────────────────────────────────
    // Event Handlers
    // ─────────────────────────────────────────────────────

    private void OnSnapshotReceived(
        object? sender,
        SystemSnapshot snapshot)
    {
        Dashboard.OnSnapshotReceived(snapshot);
    }

    private void OnPluginError(
        object? sender,
        PluginErrorEventArgs e)
    {
        Plugins.OnPluginError(
            e.PluginName,
            e.Exception.Message);
    }

    private void OnAlertTriggered(
        object? sender,
        AlertEventArgs e)
    {
        Dashboard.ShowAlert(e.Message);
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        await StartMonitoringAsync();
    }

    public async Task ShutdownAsync()
    {
        _service.SnapshotReady -= OnSnapshotReceived;
        _service.PluginError -= OnPluginError;
        _alertPlugin.AlertTriggered -= OnAlertTriggered;

        await _service.StopAsync();
    }
}