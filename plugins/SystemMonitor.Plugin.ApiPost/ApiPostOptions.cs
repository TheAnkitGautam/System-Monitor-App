namespace SystemMonitor.Plugin.ApiPost;

/// <summary>Configuration for the API-post plugin.</summary>
public sealed class ApiPostOptions
{
    public const string SectionName = "Plugins:ApiPost";

    /// <summary>Whether the plugin is enabled on startup.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Full URL of the REST endpoint that receives the JSON payload.
    /// Must be configured before the plugin will post successfully.
    /// </summary>
    public string Endpoint { get; set; } = "https://httpbin.org/post";

    /// <summary>HTTP request timeout in seconds. Defaults to 10.</summary>
    public int TimeoutSeconds { get; set; } = 10;
}
