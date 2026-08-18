using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// Guild listing and member lookup.
/// </summary>
public class GuildService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GuildService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<GuildMemberInfo>> GetMembersAsync(string guildName)
    {
        if (string.IsNullOrWhiteSpace(guildName))
            return new List<GuildMemberInfo>();

        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.GuildMembers
            .AsNoTracking()
            .Include(m => m.Guild)
            .Include(m => m.Character)
            .Where(m => m.Guild != null && m.Guild.Name == guildName)
            .Select(m => new GuildMemberInfo
            {
                Name = m.Character != null ? m.Character.Name : "",
                Status = m.Status,
            })
            .ToListAsync();
    }
}

public class GuildMemberInfo
{
    public string Name { get; set; } = string.Empty;
    public short Status { get; set; }
}
