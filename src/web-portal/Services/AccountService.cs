using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// Account registration, authentication and password management.
/// Triển khai Dual-Write pattern:
/// - Season 6 (OpenMU / PostgreSQL 15): BCrypt (work factor 11)
/// - Season 16 (LgdMu / MySQL 5.6): SHA-1 (account + ":" + password)
/// </summary>
public class AccountService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IS16AccountRepository _s16Repo;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IDbContextFactory<AppDbContext> dbFactory,
        IS16AccountRepository s16Repo,
        ILogger<AccountService> logger)
    {
        _dbFactory = dbFactory;
        _s16Repo = s16Repo;
        _logger = logger;
    }

    /// <summary>Attempts to register a new account with Dual-Write to S6 (PG) and S16 (MySQL). Returns null on success, or an error message.</summary>
    public async Task<string?> RegisterAsync(string loginName, string email, string password, string repeatPassword)
    {
        if (string.IsNullOrWhiteSpace(loginName) || loginName.Length is < 4 or > 10)
        {
            return "Username must be between 4 and 10 characters.";
        }

        if (!IsValidEmail(email))
        {
            return "A valid email is required.";
        }

        if (password.Length is < 8 or > 20)
        {
            return "Password must be between 8 and 20 characters.";
        }

        if (password != repeatPassword)
        {
            return "Passwords do not match.";
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var cleanUsername = loginName.Trim();
        var cleanEmail = email.Trim();

        // 1. Kiểm tra trùng lặp trên S6 (PostgreSQL)
        if (await db.Accounts.AnyAsync(a => a.LoginName.ToLower() == cleanUsername.ToLower()))
        {
            return "Username already in use.";
        }

        if (await db.Accounts.AnyAsync(a => a.EMail.ToLower() == cleanEmail.ToLower()))
        {
            return "Email already in use.";
        }

        // 2. Kiểm tra trùng lặp trên S16 (MySQL)
        if (await _s16Repo.AccountExistsAsync(loginName))
        {
            return "Username already in use on Season 16.";
        }

        // 3. Ghi vào PostgreSQL 15 (S6) trước
        var s6Account = new Account
        {
            Id = Guid.NewGuid(),
            LoginName = loginName,
            EMail = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 11),
            SecurityCode = string.Empty,
            VaultPassword = string.Empty,
            IsVaultExtended = false,
            State = 0,
            TimeZone = 0,
            RegistrationDate = DateTime.UtcNow,
        };

        db.Accounts.Add(s6Account);
        await db.SaveChangesAsync();

        // 4. Ghi song song vào MySQL 5.6 (S16)
        var s16HashedPassword = S16PasswordHasher.Hash(loginName, password);
        bool s16Success = false;
        try
        {
            s16Success = await _s16Repo.CreateAccountAsync(loginName, s16HashedPassword, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Direct write to S16 MySQL failed for '{Username}'", loginName);
        }

        // 5. Nếu ghi MySQL thất bại, thêm vào hàng đợi retry (AccountSyncQueue)
        if (!s16Success)
        {
            _logger.LogWarning("Enqueueing S16 sync retry for '{Username}'", loginName);
            db.AccountSyncQueues.Add(new AccountSyncQueue
            {
                Username = loginName,
                HashedPasswordS16 = s16HashedPassword,
                Email = email,
                Action = "create",
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0,
                LastError = "Direct write failed during registration"
            });
            await db.SaveChangesAsync();
        }

        return null;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try { _ = new System.Net.Mail.MailAddress(email); return true; }
        catch { return false; }
    }

    public async Task<Account?> AuthenticateAsync(string loginName, string password)
    {
        if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var cleanUsername = loginName.Trim();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.LoginName.ToLower() == cleanUsername.ToLower());
        if (account is null)
        {
            return null;
        }

        return BCrypt.Net.BCrypt.Verify(password, account.PasswordHash) ? account : null;
    }

    public async Task<bool> IsGameMasterAsync(Guid accountId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Characters.AnyAsync(c => c.AccountId == accountId && c.CharacterStatus == 32);
    }

    /// <summary>Gets an account by ID.</summary>
    public async Task<Account?> GetAccountByIdAsync(Guid accountId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
    }

    /// <summary>Changes the account password across both S6 and S16 databases.</summary>
    public async Task<string?> ChangePasswordAsync(Guid accountId, string oldPassword, string newPassword, string repeatNewPassword)
    {
        if (oldPassword == newPassword)
        {
            return "New password must differ from the old password.";
        }

        if (newPassword != repeatNewPassword)
        {
            return "New password and confirmation do not match.";
        }

        if (newPassword.Length is < 8 or > 20)
        {
            return "Password must be between 8 and 20 characters.";
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        if (account is null)
        {
            return "Account not found.";
        }

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, account.PasswordHash))
        {
            return "The old password is incorrect.";
        }

        // 1. Cập nhật S6 (PostgreSQL)
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 11);
        await db.SaveChangesAsync();

        // 2. Cập nhật S16 (MySQL)
        var s16HashedPassword = S16PasswordHasher.Hash(account.LoginName, newPassword);
        bool s16Success = false;
        try
        {
            s16Success = await _s16Repo.UpdatePasswordAsync(account.LoginName, s16HashedPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Direct password update to S16 MySQL failed for '{Username}'", account.LoginName);
        }

        // 3. Fallback vào queue retry nếu MySQL thất bại
        if (!s16Success)
        {
            _logger.LogWarning("Enqueueing S16 password sync retry for '{Username}'", account.LoginName);
            db.AccountSyncQueues.Add(new AccountSyncQueue
            {
                Username = account.LoginName,
                HashedPasswordS16 = s16HashedPassword,
                Email = account.EMail,
                Action = "update_password",
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0,
                LastError = "Direct password update failed"
            });
            await db.SaveChangesAsync();
        }

        return null;
    }
}