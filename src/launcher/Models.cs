using System.Text.Json.Serialization;

namespace MuLauncher;

public class ServerConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("host")]
    public string Host { get; set; } = "127.0.0.1";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 55901;

    [JsonPropertyName("clientPath")]
    public string ClientPath { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;

    [JsonPropertyName("badge")]
    public string Badge { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#3B82F6";

    [JsonIgnore]
    public bool IsOnline { get; set; } = false;

    [JsonIgnore]
    public long PingMs { get; set; } = -1;
}

public class LauncherConfig
{
    [JsonPropertyName("servers")]
    public List<ServerConfig> Servers { get; set; } = new();
}