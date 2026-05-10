using System.Text.Json;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Interfaces;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Plugin.FileLogger;

/// <summary>
/// Plugin that appends every system snapshot as a JSON line (JSONL format) to a local file.
/// Supports optional log rotation when the file exceeds <see cref="FileLoggerOptions.MaxFileSizeMb"/>.
///
/// <para>
/// File I/O errors are caught and tracked. After 5 consecutive failures the plugin
/// disables itself to avoid flooding the error log.
/// </para>
/// </summary>
public sealed class FileLoggerPlugin : IMonitorPlugin
{
    private readonly FileLoggerOptions _options;
    private readonly SemaphoreSlim     _writeLock = new(1, 1);
    private int _consecutiveFailures;
    private const int MaxConsecutiveFailures = 5;

    public string Name      => "File Logger";
    public bool   IsEnabled { get; set; }

    public FileLoggerPlugin(IOptions<FileLoggerOptions> options)
    {
        _options  = options.Value;
        IsEnabled = _options.Enabled;
    }

    /// <inheritdoc/>
    public async Task OnMetricsCollectedAsync(SystemSnapshot snapshot)
    {
        if (!IsEnabled) return;

        // Use a semaphore to prevent concurrent writes on overlapping ticks
        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await WriteSnapshotAsync(snapshot).ConfigureAwait(false);
            _consecutiveFailures = 0; // reset on success
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= MaxConsecutiveFailures)
            {
                IsEnabled = false;
                throw new InvalidOperationException(
                    $"FileLoggerPlugin disabled after {MaxConsecutiveFailures} consecutive failures. " +
                    $"Last error: {ex.Message}", ex);
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
        string filePath = Path.IsPathRooted(_options.FilePath)
            ? _options.FilePath
            : Path.Combine(AppContext.BaseDirectory, _options.FilePath);

        // Ensure the directory exists
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // Rotate if over size limit
        if (_options.MaxFileSizeMb > 0 && File.Exists(filePath))
        {
            var info = new FileInfo(filePath);
            if (info.Length > _options.MaxFileSizeMb * 1024L * 1024L)
                RotateLog(filePath);
        }

        var entry = new
        {
            timestamp = snapshot.Timestamp.ToString("O"),
            cpu       = snapshot.CpuPercent,
            ram_used  = snapshot.RamUsedMb,
            ram_total = snapshot.RamTotalMb,
            disk_used = snapshot.DiskUsedMb,
            disk_total= snapshot.DiskTotalMb
        };

        string json = JsonSerializer.Serialize(entry);
        await File.AppendAllTextAsync(filePath, json + Environment.NewLine)
                  .ConfigureAwait(false);
    }

    private static void RotateLog(string filePath)
    {
        string rotated = filePath + "." + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak";
        File.Move(filePath, rotated);
    }
}
