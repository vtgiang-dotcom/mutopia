using System.Security.Cryptography;
using System.Text;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// Password hasher cho Season 16 C++ Server (LgdMu).
/// Khớp với pattern chuẩn của LgdMu: SHA-1 (account + ":" + password).
/// </summary>
public static class S16PasswordHasher
{
    public static string Hash(string account, string password)
    {
        var input = $"{account}:{password}";
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}