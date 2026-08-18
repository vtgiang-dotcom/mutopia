namespace OpenMU.PlayerWeb.Services;

public interface IS16AccountRepository
{
    Task<bool> AccountExistsAsync(string username);
    Task<bool> CreateAccountAsync(string username, string hashedPassword, string email, string securityCode = "123456");
    Task<bool> UpdatePasswordAsync(string username, string hashedPassword);
}