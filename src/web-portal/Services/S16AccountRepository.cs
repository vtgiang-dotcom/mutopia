using MySqlConnector;

namespace OpenMU.PlayerWeb.Services;

public class S16AccountRepository : IS16AccountRepository
{
    private readonly string _connectionString;
    private readonly ILogger<S16AccountRepository> _logger;

    public S16AccountRepository(IConfiguration configuration, ILogger<S16AccountRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("S16MySQL")
            ?? throw new InvalidOperationException("Connection string 'S16MySQL' not found.");
        _logger = logger;
    }

    public async Task<bool> AccountExistsAsync(string username)
    {
        try
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "SELECT COUNT(1) FROM accounts WHERE account = @account", conn);
            cmd.Parameters.AddWithValue("@account", username);

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if S16 account exists for username '{Username}'", username);
            return false;
        }
    }

    public async Task<bool> CreateAccountAsync(string username, string hashedPassword, string email, string securityCode = "123456")
    {
        try
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var trans = await conn.BeginTransactionAsync();
            try
            {
                // 1. Insert into accounts
                await using var cmdAccount = new MySqlCommand(
                    @"INSERT INTO accounts (account, password, blocked, security_code, golden_channel, facebook_status, secured, email)
                      VALUES (@account, @password, 0, @secCode, 0, 0, 1, @email);
                      SELECT LAST_INSERT_ID();", conn, trans);
                cmdAccount.Parameters.AddWithValue("@account", username);
                cmdAccount.Parameters.AddWithValue("@password", hashedPassword);
                cmdAccount.Parameters.AddWithValue("@secCode", securityCode);
                cmdAccount.Parameters.AddWithValue("@email", email);

                var guid = Convert.ToInt32(await cmdAccount.ExecuteScalarAsync());

                // 2. Insert into accounts_security
                await using var cmdSec = new MySqlCommand(
                    @"INSERT IGNORE INTO accounts_security (account_id, account, ip, mac, disk_serial)
                      VALUES (@guid, @account, '127.0.0.1', '', '');", conn, trans);
                cmdSec.Parameters.AddWithValue("@guid", guid);
                cmdSec.Parameters.AddWithValue("@account", username);
                await cmdSec.ExecuteNonQueryAsync();

                // 3. Insert into accounts_status
                await using var cmdStatus = new MySqlCommand(
                    @"INSERT IGNORE INTO accounts_status (account_id, server_group, online)
                      VALUES (@guid, 0, 0);", conn, trans);
                cmdStatus.Parameters.AddWithValue("@guid", guid);
                await cmdStatus.ExecuteNonQueryAsync();

                await trans.CommitAsync();
                _logger.LogInformation("Successfully created S16 account for '{Username}' with GUID {Guid}", username, guid);
                return true;
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                _logger.LogError(ex, "Transaction failed while creating S16 account for '{Username}'", username);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect/write S16 account for '{Username}'", username);
            return false;
        }
    }

    public async Task<bool> UpdatePasswordAsync(string username, string hashedPassword)
    {
        try
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "UPDATE accounts SET password = @password WHERE account = @account", conn);
            cmd.Parameters.AddWithValue("@password", hashedPassword);
            cmd.Parameters.AddWithValue("@account", username);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update S16 account password for '{Username}'", username);
            return false;
        }
    }
}