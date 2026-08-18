using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// Background worker xử lý retry đồng bộ tài khoản sang MySQL Season 16 (LgdMu)
/// khi có lỗi ghi đồng thời (Dual-Write) từ Web Portal.
/// Chạy định kỳ mỗi 30 giây.
/// </summary>
public class AccountSyncRetryService : BackgroundService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AccountSyncRetryService> _logger;

    public AccountSyncRetryService(
        IDbContextFactory<AppDbContext> dbFactory,
        IServiceProvider serviceProvider,
        ILogger<AccountSyncRetryService> logger)
    {
        _dbFactory = dbFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AccountSyncRetryService is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingSyncQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing AccountSyncRetryService.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessPendingSyncQueueAsync(CancellationToken stoppingToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(stoppingToken);

        var pendingItems = await db.AccountSyncQueues
            .Where(q => q.RetryCount < 10)
            .OrderBy(q => q.CreatedAt)
            .Take(20)
            .ToListAsync(stoppingToken);

        if (pendingItems.Count == 0)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var s16Repo = scope.ServiceProvider.GetRequiredService<IS16AccountRepository>();

        foreach (var item in pendingItems)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            bool success = false;
            try
            {
                if (item.Action == "create")
                {
                    success = await s16Repo.CreateAccountAsync(item.Username, item.HashedPasswordS16, item.Email);
                }
                else if (item.Action == "update_password")
                {
                    success = await s16Repo.UpdatePasswordAsync(item.Username, item.HashedPasswordS16);
                }
            }
            catch (Exception ex)
            {
                item.LastError = ex.Message;
            }

            if (success)
            {
                _logger.LogInformation("Successfully synced account '{Username}' to S16 via retry worker.", item.Username);
                db.AccountSyncQueues.Remove(item);
            }
            else
            {
                item.RetryCount++;
                _logger.LogWarning("Retry {Count}/10 failed for syncing account '{Username}' to S16.", item.RetryCount, item.Username);
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }
}