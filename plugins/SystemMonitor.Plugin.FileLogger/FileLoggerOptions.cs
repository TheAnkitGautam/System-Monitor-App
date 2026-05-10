namespace SystemMonitor.Plugin.FileLogger;
public sealed class FileLoggerOptions
{
    public const string SectionName = "Plugins:FileLogger";
    public bool Enabled { get; set; }
    public string? FilePath { get; set; }
    public int MaxFileSizeMb { get; set; }
}
