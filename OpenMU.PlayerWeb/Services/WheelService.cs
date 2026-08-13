using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

public record WheelPrize(string Name, short Tier, double Probability);

public class WheelService
{
    private static readonly WheelPrize[] PrizeTable =
    [
        new("Jewel of Bless x5",  0, 0.30),
        new("Jewel of Soul x5",   0, 0.25),
        new("Jewel of Life x3",   0, 0.20),
        new("Jewel of Chaos x10", 0, 0.15),
        new("Exc Item (Random)",  1, 0.08),
        new("Bonus Spin x1",      2, 0.02),
    ];

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public WheelService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<(string? Error, WheelPrize? Prize)> SpinAsync(Guid accountId, Guid characterId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (!await db.Accounts.AnyAsync(a => a.Id == accountId))
        {
            return ("Account not found.", null);
        }

        var roll = Random.Shared.NextDouble();
        var cumulative = 0.0;
        WheelPrize? prize = null;
        foreach (var p in PrizeTable)
        {
            cumulative += p.Probability;
            if (roll <= cumulative)
            {
                prize = p;
                break;
            }
        }

        prize ??= PrizeTable[0];

        var updated = await db.Accounts
            .Where(a => a.Id == accountId && a.WheelSpins > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.WheelSpins, a => a.WheelSpins - 1));

        if (updated == 0)
        {
            return ("No spins remaining.", null);
        }

        if (prize.Name.StartsWith("Bonus Spin"))
        {
            await db.Accounts
                .Where(a => a.Id == accountId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.WheelSpins, a => a.WheelSpins + 1));
        }

        db.WheelSpins.Add(new WheelSpin
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            CharacterId = characterId,
            SpunAt = DateTime.UtcNow,
            Prize = prize.Name,
            PrizeTier = prize.Tier,
        });

        await db.SaveChangesAsync();
        return (null, prize);
    }

    public async Task<int> GetSpinsRemainingAsync(Guid accountId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId);
        return account?.WheelSpins ?? 0;
    }

    public async Task<List<WheelSpin>> GetHistoryAsync(Guid accountId, int limit = 10)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WheelSpins
            .AsNoTracking()
            .Where(w => w.AccountId == accountId)
            .OrderByDescending(w => w.SpunAt)
            .Take(limit)
            .ToListAsync();
    }
}
