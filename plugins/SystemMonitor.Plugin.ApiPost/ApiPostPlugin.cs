using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Plugin.ApiPost;

public sealed class ApiPostPlugin : IMonitorPlugin
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ApiPostOptions> _options;

    public string Name => "API Post";

    public bool IsEnabled { get; set; }

    public ApiPostPlugin(
        HttpClient httpClient,
        IOptionsMonitor<ApiPostOptions> options)
    {
        _httpClient = httpClient;
        _options = options;

        // Apply initial settings
        ApplyOptions(_options.CurrentValue);

        // React to runtime config changes
        _options.OnChange(ApplyOptions);
    }

    public async Task OnMetricsCollectedAsync(SystemSnapshot snapshot)
    {
        var options = _options.CurrentValue;

        if (!IsEnabled)
            return;

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException(
                "ApiPostPlugin: Endpoint is not configured in appsettings.json.");
        }

        var payload = new
        {
            timestamp = snapshot.Timestamp,
            cpu = snapshot.CpuPercent,
            ram_used = snapshot.RamUsedMb,
            ram_total = snapshot.RamTotalMb,
            disk_used = snapshot.DiskUsedMb,
            disk_total = snapshot.DiskTotalMb
        };

        using var response = await _httpClient
            .PostAsJsonAsync(options.Endpoint, payload)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }

    private void ApplyOptions(ApiPostOptions options)
    {
        IsEnabled = options.Enabled;

        _httpClient.Timeout =
            TimeSpan.FromSeconds(options.TimeoutSeconds);
    }
}