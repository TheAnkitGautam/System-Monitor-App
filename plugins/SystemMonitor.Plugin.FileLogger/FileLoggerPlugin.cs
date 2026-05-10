using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Plugin.FileLogger;

public sealed class FileLoggerPlugin : IMonitorPlugin
{
    private readonly IOptionsMonitor<FileLoggerOptions> _options;

    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private int _consecutiveFailures;

    private const int MaxConsecutiveFailures = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string Name => "File Logger";

    public bool IsEnabled { get; set; }

    public FileLoggerPlugin(
        IOptionsMonitor<FileLoggerOptions> options)
    {
        _options = options;

        // Apply initial settings
        ApplyOptions(_options.CurrentValue);

        // React to runtime config changes
        _options.OnChange(ApplyOptions);
    }

    public async Task OnMetricsCollectedAsync(SystemSnapshot snapshot)
    {
        if (!IsEnabled)
            return;

        await _writeLock.WaitAsync().ConfigureAwait(false);

        try
        {
            await WriteSnapshotAsync(snapshot)
                .ConfigureAwait(false);

            _consecutiveFailures = 0;
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;

            if (_consecutiveFailures >= MaxConsecutiveFailures)
            {
                IsEnabled = false;

                throw new InvalidOperationException(
                    $"FileLoggerPlugin disabled after " +
                    $"{MaxConsecutiveFailures} consecutive failures. " +
                    $"Last error: {ex.Message}",
                    ex);
            }

            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task WriteSnapshotAsync(SystemSnapshot snapshot)
    {
        var options = _options.CurrentValue;

        string filePath = Path.IsPathRooted(options.FilePath)
            ? options.FilePath
            : Path.Combine(
                AppContext.BaseDirectory,
                options.FilePath);

        string? directory =
            Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Rotate log if max size exceeded
        if (options.MaxFileSizeMb > 0 &&
            File.Exists(filePath))
        {
            var info = new FileInfo(filePath);

            if (info.Length >
                options.MaxFileSizeMb * 1024L * 1024L)
            {
                RotateLog(filePath);
            }
        }

        var entry = new
        {
            timestamp = snapshot.Timestamp.ToString("O"),

            cpu = snapshot.CpuPercent,

            ram_used = snapshot.RamUsedMb,
            ram_total = snapshot.RamTotalMb,

            disk_used = snapshot.DiskUsedMb,
            disk_total = snapshot.DiskTotalMb
        };

        string json = JsonSerializer.Serialize(
            entry,
            JsonOptions);

        await File.AppendAllTextAsync(
                filePath,
                json + Environment.NewLine)
            .ConfigureAwait(false);
    }

    private void ApplyOptions(FileLoggerOptions options)
    {
        IsEnabled = options.Enabled;
    }

    private static void RotateLog(string filePath)
    {
        string rotated =
            filePath + "." +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
            ".bak";

        File.Move(filePath, rotated);
    }
}