using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// Bot account listing and deletion (admin).
/// </summary>
public class BotService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BotService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<BotInfo>> GetBotsAsync(int limit = 100)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var accounts = await db.Accounts
            .AsNoTracking()
            .Include(a => a.Characters).ThenInclude(c => c.CharacterClass)
            .Include(a => a.Characters).ThenInclude(c => c.GameMapDefinition)
            .Where(a => a.LoginName.StartsWith("bot"))
            .OrderByDescending(a => a.RegistrationDate)
            .Take(limit)
            .ToListAsync();

        return accounts.Select(a =>
        {
            var character = a.Characters.FirstOrDefault();
            return new BotInfo
            {
                Id = a.Id,
                LoginName = a.LoginName,
                RegistrationDate = a.RegistrationDate,
                CharacterName = character?.Name ?? "N/A",
                ClassName = character?.CharacterClass?.Name ?? "N/A",
                MapName = character?.GameMapDefinition?.Name ?? "Lorencia",
            };
        }).ToList();
    }

    public async Task<int> DeleteAllBotsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var bots = await db.Accounts.Where(a => a.LoginName.StartsWith("bot")).ToListAsync();
        db.Accounts.RemoveRange(bots);
        return await db.SaveChangesAsync();
    }
}

public class BotInfo
{
    public Guid Id { get; set; }
    public string LoginName { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
}
