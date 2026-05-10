using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemMonitor.AppLogic.Configuration;
using SystemMonitor.Plugin.ApiPost;
using SystemMonitor.Plugin.FileLogger;
using SystemMonitor.Plugin.ThresholdAlert;
using Microsoft.Extensions.Options;
using System.IO;

namespace SystemMonitor.WPF.ViewModels;

/// <summary>
/// ViewModel for the Settings view. Exposes all configurable values as two-way
/// bindable properties. On <see cref="SaveCommand"/>, writes changes back to
/// appsettings.json so they persist across restarts.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    // ── Monitoring ───────────────────────────────────────────────────────────
    [ObservableProperty] private int _intervalSeconds;

    // ── File Logger ──────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _fileLoggerEnabled;
    [ObservableProperty] private string _fileLoggerPath = string.Empty;
    [ObservableProperty] private int    _fileLoggerMaxSizeMb;

    // ── API Post ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _apiPostEnabled;
    [ObservableProperty] private string _apiEndpoint = string.Empty;
    [ObservableProperty] private int    _apiTimeoutSeconds;

    // ── Threshold Alert ──────────────────────────────────────────────────────
    [ObservableProperty] private bool   _alertEnabled;
    [ObservableProperty] private double _cpuThreshold;
    [ObservableProperty] private double _ramThreshold;
    [ObservableProperty] private double _diskThreshold;
    [ObservableProperty] private int    _cooldownSeconds;

    [ObservableProperty] private string _saveStatus = string.Empty;

    public SettingsViewModel(
        IOptions<MonitoringOptions>      monitoringOpts,
        IOptions<FileLoggerOptions>      fileOpts,
        IOptions<ApiPostOptions>         apiOpts,
        IOptions<ThresholdAlertOptions>  alertOpts)
    {
        // Load current values
        IntervalSeconds      = monitoringOpts.Value.IntervalSeconds;

        FileLoggerEnabled    = fileOpts.Value.Enabled;
        FileLoggerPath       = fileOpts.Value.FilePath;
        FileLoggerMaxSizeMb  = fileOpts.Value.MaxFileSizeMb;

        ApiPostEnabled       = apiOpts.Value.Enabled;
        ApiEndpoint          = apiOpts.Value.Endpoint;
        ApiTimeoutSeconds    = apiOpts.Value.TimeoutSeconds;

        AlertEnabled         = alertOpts.Value.Enabled;
        CpuThreshold         = alertOpts.Value.CpuThreshold;
        RamThreshold         = alertOpts.Value.RamThresholdPercent;
        DiskThreshold        = alertOpts.Value.DiskThresholdPercent;
        CooldownSeconds      = alertOpts.Value.CooldownSeconds;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            // Read existing JSON to preserve any keys we don't manage
            string raw  = File.Exists(path) ? await File.ReadAllTextAsync(path) : "{}";
            var    root = JsonNode.Parse(raw) as JsonObject ?? new JsonObject();

            // Write back all sections
            root["Monitoring"] = new JsonObject
            {
                ["IntervalSeconds"] = IntervalSeconds
            };
            root["Plugins"] = new JsonObject
            {
                ["FileLogger"] = new JsonObject
                {
                    ["Enabled"]      = FileLoggerEnabled,
                    ["FilePath"]     = FileLoggerPath,
                    ["MaxFileSizeMb"]= FileLoggerMaxSizeMb
                },
                ["ApiPost"] = new JsonObject
                {
                    ["Enabled"]        = ApiPostEnabled,
                    ["Endpoint"]       = ApiEndpoint,
                    ["TimeoutSeconds"] = ApiTimeoutSeconds
                },
                ["ThresholdAlert"] = new JsonObject
                {
                    ["Enabled"]              = AlertEnabled,
                    ["CpuThreshold"]         = CpuThreshold,
                    ["RamThresholdPercent"]  = RamThreshold,
                    ["DiskThresholdPercent"] = DiskThreshold,
                    ["CooldownSeconds"]      = CooldownSeconds
                }
            };

            var writeOptions = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(path, root.ToJsonString(writeOptions));

            SaveStatus = $"Saved at {DateTime.Now:HH:mm:ss}. Restart to apply interval changes.";
        }
        catch (Exception ex)
        {
            SaveStatus = $"Save failed: {ex.Message}";
        }
    }
}
