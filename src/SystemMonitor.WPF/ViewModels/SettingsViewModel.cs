using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using SystemMonitor.AppLogic.Configuration;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Plugin.ApiPost;
using SystemMonitor.Plugin.FileLogger;
using SystemMonitor.Plugin.ThresholdAlert;

namespace SystemMonitor.WPF.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    // Monitoring
    [ObservableProperty] private int _intervalSeconds;

    // File Logger
    [ObservableProperty] private bool _fileLoggerEnabled;
    [ObservableProperty] private string _fileLoggerPath = string.Empty;
    [ObservableProperty] private int _fileLoggerMaxSizeMb;

    // API Post         
    [ObservableProperty] private bool _apiPostEnabled;
    [ObservableProperty] private string _apiEndpoint = string.Empty;
    [ObservableProperty] private int _apiTimeoutSeconds;

    // Threshold Alert
    [ObservableProperty] private bool _alertEnabled;
    [ObservableProperty] private double _cpuThreshold;
    [ObservableProperty] private double _ramThreshold;
    [ObservableProperty] private double _diskThreshold;
    [ObservableProperty] private int _cooldownSeconds;

    [ObservableProperty] private string _saveStatus = string.Empty;

    private readonly IMonitoringService _monitoringService;

    public SettingsViewModel(
        IOptionsMonitor<MonitoringOptions> monitoringOpts,
        IOptionsMonitor<FileLoggerOptions> fileOpts,
        IOptionsMonitor<ApiPostOptions> apiOpts,
        IOptionsMonitor<ThresholdAlertOptions> alertOpts,
        IMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;

        // Monitoring
        IntervalSeconds = monitoringOpts.CurrentValue.IntervalSeconds;
        // File Logger
        FileLoggerEnabled = fileOpts.CurrentValue.Enabled;
        FileLoggerPath = fileOpts.CurrentValue.FilePath;
        FileLoggerMaxSizeMb = fileOpts.CurrentValue.MaxFileSizeMb;

        // API Post
        ApiPostEnabled = apiOpts.CurrentValue.Enabled;
        ApiEndpoint = apiOpts.CurrentValue.Endpoint;
        ApiTimeoutSeconds = apiOpts.CurrentValue.TimeoutSeconds;

        // Threshold Alert
        AlertEnabled = alertOpts.CurrentValue.Enabled;
        CpuThreshold = alertOpts.CurrentValue.CpuThreshold;
        RamThreshold = alertOpts.CurrentValue.RamThresholdPercent;
        DiskThreshold = alertOpts.CurrentValue.DiskThresholdPercent;
        CooldownSeconds = alertOpts.CurrentValue.CooldownSeconds;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            // Read existing JSON to preserve any keys we don't manage
            string raw = File.Exists(path) ? await File.ReadAllTextAsync(path) : "{}";
            var root = JsonNode.Parse(raw) as JsonObject ?? new JsonObject();

            // Write back all sections
            root["Monitoring"] = new JsonObject
            {
                ["IntervalSeconds"] = IntervalSeconds
            };
            root["Plugins"] = new JsonObject
            {
                ["FileLogger"] = new JsonObject
                {
                    ["Enabled"] = FileLoggerEnabled,
                    ["FilePath"] = FileLoggerPath,
                    ["MaxFileSizeMb"] = FileLoggerMaxSizeMb
                },
                ["ApiPost"] = new JsonObject
                {
                    ["Enabled"] = ApiPostEnabled,
                    ["Endpoint"] = ApiEndpoint,
                    ["TimeoutSeconds"] = ApiTimeoutSeconds
                },
                ["ThresholdAlert"] = new JsonObject
                {
                    ["Enabled"] = AlertEnabled,
                    ["CpuThreshold"] = CpuThreshold,
                    ["RamThresholdPercent"] = RamThreshold,
                    ["DiskThresholdPercent"] = DiskThreshold,
                    ["CooldownSeconds"] = CooldownSeconds
                }
            };

            var writeOptions = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(path, root.ToJsonString(writeOptions));
            await Task.Delay(500);
            await _monitoringService.RestartAsync();
            SaveStatus = $"Saved at {DateTime.Now:HH:mm:ss}. Settings applied successfully.";
        }
        catch (Exception ex)
        {
            SaveStatus = $"Save failed: {ex.Message}";
        }
    }
}
