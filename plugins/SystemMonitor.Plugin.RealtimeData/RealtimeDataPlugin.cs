using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;
using Microsoft.Extensions.Options;

namespace SystemMonitor.Plugin.RealtimeData;

public sealed class RealtimeDataPlugin : IMonitorPlugin, IDisposable
{
    private readonly IOptionsMonitor<RealtimeDataOptions> _options;

    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private ClientWebSocket? _socket;

    private bool _disposed;

    public string Name => "Realtime Data Stream";

    public bool IsEnabled { get; set; }

    public RealtimeDataPlugin(
        IOptionsMonitor<RealtimeDataOptions> options)
    {
        _options = options;

        ApplyOptions(_options.CurrentValue);

        _options.OnChange(ApplyOptions);
    }

    public async Task OnMetricsCollectedAsync(
        SystemSnapshot snapshot)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            await EnsureConnectedAsync();

            if (_socket is null ||
                _socket.State != WebSocketState.Open)
            {
                return;
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

            string json =
                JsonSerializer.Serialize(payload);

            byte[] bytes =
                Encoding.UTF8.GetBytes(json);

            await _socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception)
        {
            await ResetConnectionAsync();
        }
    }

    private async Task EnsureConnectedAsync()
    {
        await _connectionLock.WaitAsync();

        try
        {
            if (_socket is { State: WebSocketState.Open })
            {
                return;
            }

            string url =
                _options.CurrentValue.ServerUrl;

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException(
                    "RealtimeDataPlugin: ServerUrl is empty.");
            }

            _socket?.Dispose();

            _socket = new ClientWebSocket();

            await _socket.ConnectAsync(
                new Uri(url),
                CancellationToken.None);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ResetConnectionAsync()
    {
        try
        {
            if (_socket is not null)
            {
                if (_socket.State == WebSocketState.Open)
                {
                    await _socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Reconnect",
                        CancellationToken.None);
                }

                _socket.Dispose();
            }
        }
        catch
        {
        }

        _socket = null;
    }

    private void ApplyOptions(
        RealtimeDataOptions options)
    {
        IsEnabled = options.Enabled;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _socket?.Dispose();

        _connectionLock.Dispose();

        _disposed = true;
    }
}