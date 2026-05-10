using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemMonitor.Core.Interfaces;

namespace SystemMonitor.WPF.ViewModels;

/// <summary>Wraps a single <see cref="IMonitorPlugin"/> for display in the Plugins view.</summary>
public sealed partial class PluginViewModel : ViewModelBase
{
    private readonly IMonitorPlugin _plugin;

    public string Name => _plugin.Name;

    [ObservableProperty] private bool   _isEnabled;
    [ObservableProperty] private string _status  = "Idle";
    [ObservableProperty] private string _lastRun = "Never";

    public PluginViewModel(IMonitorPlugin plugin)
    {
        _plugin   = plugin;
        _isEnabled = plugin.IsEnabled;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _plugin.IsEnabled = value;
        Status = value ? "Enabled" : "Disabled";
    }

    public void MarkSuccess()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Status  = "OK";
            LastRun = DateTime.Now.ToString("HH:mm:ss");
        });
    }

    public void MarkError(string errorMessage)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Status  = $"Error: {errorMessage}";
            LastRun = DateTime.Now.ToString("HH:mm:ss");
        });
    }
}

/// <summary>ViewModel for the Plugins management view.</summary>
public sealed partial class PluginsViewModel : ViewModelBase
{
    public ObservableCollection<PluginViewModel> Plugins { get; } = new();

    public PluginsViewModel(IEnumerable<IMonitorPlugin> plugins)
    {
        foreach (var plugin in plugins)
            Plugins.Add(new PluginViewModel(plugin));
    }

    [RelayCommand]
    private void EnableAll()
    {
        foreach (var p in Plugins) p.IsEnabled = true;
    }

    [RelayCommand]
    private void DisableAll()
    {
        foreach (var p in Plugins) p.IsEnabled = false;
    }

    /// <summary>Called by MainViewModel when a plugin-level error is reported.</summary>
    public void OnPluginError(string pluginName, string error)
    {
        var vm = Plugins.FirstOrDefault(p => p.Name == pluginName);
        vm?.MarkError(error);
    }
}
