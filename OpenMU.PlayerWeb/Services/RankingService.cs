using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// Ranking queries: killers, resets, online, guilds.
/// </summary>
public class RankingService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ServerStatusService _serverStatus;

    public RankingService(IDbContextFactory<AppDbContext> dbFactory, ServerStatusService serverStatus)
    {
        _dbFactory = dbFactory;
        _serverStatus = serverStatus;
    }

    public async Task<List<KillerRank>> GetKillersAsync(int limit = 30)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Characters
            .AsNoTracking()
            .OrderByDescending(c => c.PlayerKillCount)
            .Take(limit)
            .Select(c => new KillerRank { Name = c.Name, PlayerKillCount = c.PlayerKillCount, ClassName = c.CharacterClass != null ? c.CharacterClass.Name : "" })
            .ToListAsync();
    }

    public async Task<List<ResetRank>> GetResetRankingAsync(int limit = 50)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var levelId = OpenMuConstants.Attributes.Level;
        var resetId = OpenMuConstants.Attributes.Reset;
        var masterId = OpenMuConstants.Attributes.MasterLevel;

        var rows = await db.StatAttributes
            .AsNoTracking()
            .Where(s => s.CharacterId.HasValue && (s.DefinitionId == levelId || s.DefinitionId == resetId || s.DefinitionId == masterId))
            .GroupBy(s => s.CharacterId!.Value)
            .Select(g => new
            {
                CharacterId = g.Key,
                Resets = g.Where(x => x.DefinitionId == resetId).Max(x => (float?)x.Value) ?? 0,
                Level = g.Where(x => x.DefinitionId == levelId).Max(x => (float?)x.Value) ?? 0,
                MasterLevel = g.Where(x => x.DefinitionId == masterId).Max(x => (float?)x.Value) ?? 0,
            })
            .OrderByDescending(x => x.Resets)
            .ThenByDescending(x => x.Level)
            .ThenByDescending(x => x.MasterLevel)
            .Take(limit)
            .ToListAsync();

        var characterIds = rows.Select(r => r.CharacterId).ToList();
        var names = await db.Characters
            .AsNoTracking()
            .Where(c => characterIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        return rows.Select(r => new ResetRank
        {
            Name = names.GetValueOrDefault(r.CharacterId, "Unknown"),
            Resets = (int)r.Resets,
            Level = (int)r.Level,
            MasterLevel = (int)r.MasterLevel,
        }).ToList();
    }

    public async Task<List<OnlineRank>> GetOnlineRankingAsync()
    {
        var players = await _serverStatus.GetOnlinePlayersAsync();
        if (players is null || players.Count == 0)
        {
            return new List<OnlineRank>();
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Characters
            .AsNoTracking()
            .Where(c => players.Contains(c.Name))
            .Select(c => new OnlineRank
            {
                Name = c.Name,
                MapName = c.GameMapDefinition != null && c.GameMapDefinition.Name != null ? c.GameMapDefinition.Name : "",
                ClassName = c.CharacterClass != null ? c.CharacterClass.Name : "",
            })
            .ToListAsync();
    }

    public async Task<List<GuildRank>> GetGuildsAsync(int limit = 30)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Guilds
            .AsNoTracking()
            .OrderByDescending(g => g.Score)
            .Take(limit)
            .Select(g => new GuildRank { Name = g.Name, Score = g.Score })
            .ToListAsync();
    }
}

public class KillerRank
{
    public string Name { get; set; } = string.Empty;
    public int PlayerKillCount { get; set; }
    public string ClassName { get; set; } = string.Empty;
}

public class ResetRank
{
    public string Name { get; set; } = string.Empty;
    public int Resets { get; set; }
    public int Level { get; set; }
    public int MasterLevel { get; set; }
}

public class OnlineRank
{
    public string Name { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
}

public class GuildRank
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
}
