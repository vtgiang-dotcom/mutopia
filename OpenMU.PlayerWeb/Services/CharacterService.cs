using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// Character management: reset, add stats, pk clear, reset stats.
/// Mirrors the original Next.js API logic (same stat/map UUIDs and checks).
/// </summary>
public class CharacterService
{
    // Stat attribute definition UUIDs (fixed by OpenMU).
    private static readonly Guid StatStr = Guid.Parse("123282fe-fead-448e-ad2c-baece939b4b1");
    private static readonly Guid StatAgi = Guid.Parse("1ae9c014-e3cd-4703-bd05-1b65f5f94ceb");
    private static readonly Guid StatVit = Guid.Parse("6ca5c3a6-b109-45a5-87a7-fdcb107b4982");
    private static readonly Guid StatEne = Guid.Parse("01b0ef28-f7a0-46b5-97ba-2b624a54cd75");
    private static readonly Guid StatLead = Guid.Parse("6af2c9df-3ae4-4721-8462-9a8ec7f56fe4");

    // Level / reset / master level definitions.
    private static readonly Guid AttrLevel = OpenMuConstants.Attributes.Level;
    private static readonly Guid AttrReset = OpenMuConstants.Attributes.Reset;
    private static readonly Guid AttrMasterLevel = OpenMuConstants.Attributes.MasterLevel;

    // Home maps.
    private static readonly Guid LorenciaMap = Guid.Parse("00000300-0000-0000-0000-000000000000");
    private static readonly Guid NoriaMap = Guid.Parse("00000300-0003-0000-0000-000000000000");
    private static readonly Guid ElbelandMap = Guid.Parse("00000300-0033-0000-0000-000000000000");

    // Elf class IDs -> Noria; Summoner class IDs -> Elbeland; else Lorencia.
    private static readonly Guid[] ElfClasses =
    {
        Guid.Parse("00000040-000b-0000-0000-000000000000"),
        Guid.Parse("00000040-000a-0000-0000-000000000000"),
        Guid.Parse("00000040-0008-0000-0000-000000000000"),
    };

    private static readonly Guid[] SummonerClasses =
    {
        Guid.Parse("00000040-0017-0000-0000-000000000000"),
        Guid.Parse("00000040-0016-0000-0000-000000000000"),
        Guid.Parse("00000040-0014-0000-0000-000000000000"),
    };

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ServerStatusService _serverStatus;
    private readonly IConfiguration _config;

    public CharacterService(IDbContextFactory<AppDbContext> dbFactory, ServerStatusService serverStatus, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _serverStatus = serverStatus;
        _config = config;
    }

    /// <summary>Returns the characters owned by an account (id + name + class + map).</summary>
    public async Task<List<Character>> GetAccountCharactersAsync(Guid accountId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Characters
            .AsNoTracking()
            .Include(c => c.CharacterClass)
            .Include(c => c.GameMapDefinition)
            .Where(c => c.AccountId == accountId)
            .OrderBy(c => c.CharacterSlot)
            .ToListAsync();
    }

    /// <summary>Checks whether a character name belongs to the account.</summary>
    public async Task<bool> OwnsCharacterAsync(Guid accountId, string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Characters.AnyAsync(c => c.AccountId == accountId && c.Name == name);
    }

    /// <summary>Checks if the character is currently online. Returns null if the server is unreachable.</summary>
    private async Task<bool?> IsOnlineAsync(string name)
    {
        var players = await _serverStatus.GetOnlinePlayersAsync();
        if (players is null)
        {
            return null;
        }

        return players.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<string?> AddStatsAsync(Guid accountId, string name, int str, int agi, int vit, int ene, int lead)
    {
        var total = str + agi + vit + ene + lead;
        if (total < 0)
        {
            return "Invalid point allocation.";
        }

        if (!await OwnsCharacterAsync(accountId, name))
        {
            return "You can't do this! Try to login again.";
        }

        var online = await IsOnlineAsync(name);
        if (online == true)
        {
            return "Disconnect from your account first!";
        }

        if (online is null)
        {
            return "Couldn't reach the server, try again later.";
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name == name);
        if (character is null)
        {
            return "Character not found.";
        }

        if (character.LevelUpPoints < total)
        {
            return "You don't have enough points!";
        }

        await ApplyStatIncrementAsync(db, name, StatStr, str);
        await ApplyStatIncrementAsync(db, name, StatAgi, agi);
        await ApplyStatIncrementAsync(db, name, StatVit, vit);
        await ApplyStatIncrementAsync(db, name, StatEne, ene);
        await ApplyStatIncrementAsync(db, name, StatLead, lead);

        character.LevelUpPoints -= total;
        await db.SaveChangesAsync();
        return null;
    }

    private static async Task ApplyStatIncrementAsync(AppDbContext db, string name, Guid definitionId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        var attr = await db.StatAttributes
            .FirstOrDefaultAsync(s => s.Character != null && s.Character.Name == name && s.DefinitionId == definitionId);
        if (attr is not null)
        {
            attr.Value += amount;
        }
    }

    public async Task<string?> PkClearAsync(Guid accountId, string name)
    {
        var zen = _config.GetValue<int>("GameSettings:ZenToPkClear");
        if (zen <= 0)
        {
            return "Function disabled.";
        }

        if (!await OwnsCharacterAsync(accountId, name))
        {
            return "You can't do this! Try to login again.";
        }

        var online = await IsOnlineAsync(name);
        if (online == true)
        {
            return "Disconnect from your account first!";
        }

        if (online is null)
        {
            return "Couldn't reach the server, try again later.";
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name == name);
        if (character?.Inventory is null)
        {
            return "Character not found.";
        }

        var storage = await db.ItemStorages.FirstOrDefaultAsync(s => s.Id == character.InventoryId);
        if (storage is null || storage.Money < zen)
        {
            return $"You don't have enough zen: {zen}";
        }

        storage.Money -= zen;
        character.State = 0;
        character.StateRemainingSeconds = 0;
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> ResetAsync(Guid accountId, string name, string classId)
    {
        var zen = _config.GetValue<int>("GameSettings:ZenToReset");
        var lvlToReset = _config.GetValue<int>("GameSettings:LvlToReset");
        var maxReset = _config.GetValue<int>("GameSettings:MaxReset");

        if (zen <= 0)
        {
            return "Function disabled.";
        }

        if (!await OwnsCharacterAsync(accountId, name))
        {
            return "You can't do this! Try to login again.";
        }

        var online = await IsOnlineAsync(name);
        if (online == true)
        {
            return "Disconnect from your account first!";
        }

        if (online is null)
        {
            return "Couldn't reach the server, try again later.";
        }

        var parsedClassId = Guid.TryParse(classId, out var cid) ? cid : Guid.Empty;
        var (finalMap, posX, posY) = GetResetTarget(parsedClassId);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name == name);
        if (character is null)
        {
            return "Character not found.";
        }

        var stats = await db.StatAttributes
            .Where(s => s.CharacterId == character.Id && (s.DefinitionId == AttrLevel || s.DefinitionId == AttrReset))
            .ToListAsync();

        var level = stats.FirstOrDefault(s => s.DefinitionId == AttrLevel)?.Value ?? 0;
        var resets = stats.FirstOrDefault(s => s.DefinitionId == AttrReset)?.Value ?? 0;

        if (level < lvlToReset)
        {
            return $"You aren't level {lvlToReset}.";
        }

        if (resets >= maxReset)
        {
            return $"You are at maximum reset {maxReset}.";
        }

        var storage = await db.ItemStorages.FirstOrDefaultAsync(s => s.Id == character.InventoryId);
        if (storage is null || storage.Money < zen)
        {
            return $"You don't have enough zen: {zen}";
        }

        storage.Money -= zen;

        var resetAttr = stats.FirstOrDefault(s => s.DefinitionId == AttrReset);
        var levelAttr = stats.FirstOrDefault(s => s.DefinitionId == AttrLevel);
        if (resetAttr is not null)
        {
            resetAttr.Value += 1;
        }

        if (levelAttr is not null)
        {
            levelAttr.Value = 1;
        }

        character.CurrentMapId = finalMap;
        character.PositionX = posX;
        character.PositionY = posY;
        await db.SaveChangesAsync();
        return null;
    }

    private (Guid Map, short X, short Y) GetResetTarget(Guid classId)
    {
        if (ElfClasses.Contains(classId))
        {
            return (NoriaMap, 176, 116);
        }

        if (SummonerClasses.Contains(classId))
        {
            return (ElbelandMap, 51, 226);
        }

        return (LorenciaMap, 141, 121);
    }

    public async Task<string?> ResetStatsAsync(Guid accountId, string name)
    {
        var zen = _config.GetValue<int>("GameSettings:ZenToResetStats");
        if (zen <= 0)
        {
            return "Function disabled.";
        }

        if (!await OwnsCharacterAsync(accountId, name))
        {
            return "You can't do this! Try to login again.";
        }

        var online = await IsOnlineAsync(name);
        if (online == true)
        {
            return "Disconnect from your account first!";
        }

        if (online is null)
        {
            return "Couldn't reach the server, try again later.";
        }

        var attributeStatsId = new[] { StatStr, StatAgi, StatVit, StatEne, StatLead };

        await using var db = await _dbFactory.CreateDbContextAsync();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name == name);
        if (character is null)
        {
            return "Character not found.";
        }

        var storage = await db.ItemStorages.FirstOrDefaultAsync(s => s.Id == character.InventoryId);
        if (storage is null || storage.Money < zen)
        {
            return $"You don't have enough zen: {zen}";
        }

        var stats = await db.StatAttributes
            .Where(s => s.CharacterId == character.Id && s.DefinitionId.HasValue && attributeStatsId.Contains(s.DefinitionId.Value))
            .ToListAsync();

        var totalResetedPoints = 0f;
        foreach (var stat in stats)
        {
            totalResetedPoints += Math.Max(0, stat.Value - 20);
            stat.Value = 20;
        }

        storage.Money -= zen;
        character.LevelUpPoints += (int)totalResetedPoints;
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> RenameAsync(Guid accountId, string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || !System.Text.RegularExpressions.Regex.IsMatch(newName, @"^[a-zA-Z0-9]{3,10}$"))
        {
            return "Name must be 3-10 alphanumeric characters.";
        }

        var zen = _config.GetValue<int>("GameSettings:ZenToRename");

        if (!await OwnsCharacterAsync(accountId, oldName))
        {
            return "You can't do this! Try to login again.";
        }

        var online = await IsOnlineAsync(oldName);
        if (online == true)
        {
            return "Disconnect from your account first!";
        }

        if (online is null)
        {
            return "Couldn't reach the server, try again later.";
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var exists = await db.Characters.AnyAsync(c => c.Name == newName);
        if (exists)
        {
            return "That character name is already taken.";
        }

        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name == oldName);
        if (character is null)
        {
            return "Character not found.";
        }

        if (zen > 0)
        {
            var storage = await db.ItemStorages.FirstOrDefaultAsync(s => s.Id == character.InventoryId);
            if (storage is null || storage.Money < zen)
            {
                return $"You don't have enough zen: {zen:N0}";
            }
            storage.Money -= zen;
        }

        character.Name = newName;
        await db.SaveChangesAsync();
        return null;
    }
}
