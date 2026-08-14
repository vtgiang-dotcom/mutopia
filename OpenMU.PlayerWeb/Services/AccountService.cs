using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// Account registration, authentication and password management.
/// OpenMU stores BCrypt password hashes (work factor 11).
/// </summary>
public class AccountService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AccountService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>Attempts to register a new account. Returns null on success, or an error message.</summary>
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

        if (await db.Accounts.AnyAsync(a => a.LoginName == loginName))
        {
            return "Username already in use.";
        }

        if (await db.Accounts.AnyAsync(a => a.EMail == email))
        {
            return "Email already in use.";
        }

        var account = new Account
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

        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return null;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try { _ = new System.Net.Mail.MailAddress(email); return true; }
        catch { return false; }
    }

    /// <summary>Validates credentials. Returns the account on success, otherwise null.</summary>
    public async Task<Account?> AuthenticateAsync(string loginName, string password)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.LoginName == loginName);
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

    /// <summary>Changes the account password. Returns null on success, or an error message.</summary>
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

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 11);
        await db.SaveChangesAsync();
        return null;
    }
}
