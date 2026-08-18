using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// Queries the OpenMU server status endpoint and proxies it.
/// The game server exposes <c>/api/status</c> with an online players list.
/// </summary>
public class ServerStatusService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ServerStatusService> _logger;

    public ServerStatusService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<ServerStatusService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    /// <summary>Returns the list of online player names, or null if the server is unreachable.</summary>
    public async Task<List<string>?> GetOnlinePlayersAsync()
    {
        var baseUrl = _config["GameserverUrl"] ?? "http://localhost:8080";
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            var response = await client.GetAsync($"{baseUrl}/api/status");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("playersList", out var players) && players.ValueKind == JsonValueKind.Array)
            {
                return players.EnumerateArray()
                    .Select(p => p.GetString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Cast<string>()
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Game server unreachable at {Url}", baseUrl);
        }

        return null;
    }
}
