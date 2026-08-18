using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// News CRUD. Writes require the caller to be a GM (enforced in the page layer).
/// </summary>
public class NewsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public NewsService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<NewsItem>> GetAllAsync(int limit = 50)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NewsItems
            .AsNoTracking()
            .OrderByDescending(n => n.CreationDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<NewsItem?> GetByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NewsItems.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<NewsItem> CreateAsync(string title, string body, string author)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var news = new NewsItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Body = body,
            Author = author,
            CreationDate = DateTime.UtcNow,
        };
        db.NewsItems.Add(news);
        await db.SaveChangesAsync();
        return news;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var news = await db.NewsItems.FirstOrDefaultAsync(n => n.Id == id);
        if (news is null)
        {
            return false;
        }

        db.NewsItems.Remove(news);
        await db.SaveChangesAsync();
        return true;
    }
}
