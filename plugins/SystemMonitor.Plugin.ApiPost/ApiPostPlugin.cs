using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Plugin.ApiPost;

/// <summary>
/// Plugin that HTTP-POSTs every snapshot as a JSON payload to a configurable REST endpoint.
///
/// <para>
/// Uses a shared <see cref="HttpClient"/> instance (injected) to avoid socket exhaustion.
/// The payload format matches the exercise specification exactly:
/// <code>{"cpu": &lt;cpu_percent&gt;, "ram_used": &lt;ram_in_mb&gt;, "disk_used": &lt;disk_used_in_mb&gt;}</code>
/// </para>
/// </summary>
public sealed class ApiPostPlugin : IMonitorPlugin
{
    private readonly HttpClient       _httpClient;
    private readonly ApiPostOptions   _options;

    public string Name      => "API Post";
    public bool   IsEnabled { get; set; }

    public ApiPostPlugin(HttpClient httpClient, IOptions<ApiPostOptions> options)
    {
        _httpClient = httpClient;
        _options    = options.Value;
        IsEnabled   = _options.Enabled;

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="HttpRequestException"/> and <see cref="TaskCanceledException"/> are
    /// allowed to propagate — they will be caught by <c>MonitoringService.SafeInvokePluginAsync</c>
    /// and logged without crashing the monitoring loop.
    /// </remarks>
    public async Task OnMetricsCollectedAsync(SystemSnapshot snapshot)
    {
        if (!IsEnabled) return;

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException(
                "ApiPostPlugin: Endpoint is not configured in appsettings.json.");

        // Payload format as specified by the exercise
        var payload = new
        {
            cpu       = snapshot.CpuPercent,
            ram_used  = snapshot.RamUsedMb,
            disk_used = snapshot.DiskUsedMb
        };

        using var response = await _httpClient
            .PostAsJsonAsync(_options.Endpoint, payload)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}
