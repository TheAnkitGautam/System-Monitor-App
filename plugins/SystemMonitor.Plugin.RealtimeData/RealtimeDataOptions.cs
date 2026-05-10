namespace SystemMonitor.Plugin.RealtimeData;

public sealed class RealtimeDataOptions
{
    public const string SectionName = "Plugins:RealtimeData";
    public bool Enabled { get; set; }
    public string? ServerUrl { get; set; }
    public int ReconnectDelaySeconds { get; set; }
}