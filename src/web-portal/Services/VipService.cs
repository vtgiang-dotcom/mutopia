using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

public class VipService
{
    private static readonly float[] Multipliers = { 1.0f, 1.2f, 1.5f, 2.0f };
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public VipService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public float GetExpMultiplier(short vipTier) =>
        (vipTier >= 0 && vipTier < Multipliers.Length) ? Multipliers[vipTier] : 1.0f;

    public async Task<string?> GrantVipAsync(Guid accountId, short tier, int durationDays)
    {
        if (tier < 0 || tier > 3)
        {
            return "Invalid VIP tier (0=None, 1=Silver, 2=Gold, 3=Platinum).";
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        if (account is null)
        {
            return "Account not found.";
        }

        account.VipTier = tier;
        account.VipExpiry = durationDays < 0 ? null : DateTime.UtcNow.AddDays(durationDays);
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<bool> IsVipActiveAsync(Guid accountId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId);
        if (account is null || account.VipTier == 0)
        {
            return false;
        }

        return account.VipExpiry is null || account.VipExpiry > DateTime.UtcNow;
    }

    public async Task<List<Account>> GetActiveVipAccountsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Accounts
            .AsNoTracking()
            .Where(a => a.VipTier > 0 && (a.VipExpiry == null || a.VipExpiry > DateTime.UtcNow))
            .OrderByDescending(a => a.VipTier)
            .ToListAsync();
    }
}
