namespace SystemMonitor.Plugin.ApiPost;

/// <summary>Configuration for the API-post plugin.</summary>
public sealed class ApiPostOptions
{
    public const string SectionName = "Plugins:ApiPost";
    public bool Enabled { get; set; }
    public string? Endpoint { get; set; }
    public int TimeoutSeconds { get; set; }
}
