namespace SystemMonitor.Plugin.FileLogger;

/// <summary>Configuration for the file-logger plugin.</summary>
public sealed class FileLoggerOptions
{
    public const string SectionName = "Plugins:FileLogger";

    /// <summary>Whether the plugin is enabled on startup.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Path to the log file. Relative paths are resolved from the application directory.
    /// Defaults to <c>logs/metrics.jsonl</c>.
    /// </summary>
    public string FilePath { get; set; } = "logs/metrics.jsonl";

    /// <summary>
    /// Maximum file size in megabytes before the log is rotated.
    /// Set to 0 to disable rotation. Defaults to 10 MB.
    /// </summary>
    public int MaxFileSizeMb { get; set; } = 10;
}
