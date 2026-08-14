using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

/// <summary>
/// Represents an account from the OpenMU <c>data.Account</c> table.
/// </summary>
[Table("Account", Schema = "data")]
public class Account
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("LoginName")]
    public string LoginName { get; set; } = string.Empty;

    [Column("PasswordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("SecurityCode")]
    public string SecurityCode { get; set; } = string.Empty;

    [Column("EMail")]
    public string EMail { get; set; } = string.Empty;

    [Column("RegistrationDate")]
    public DateTime RegistrationDate { get; set; }

    [Column("State")]
    public int State { get; set; }

    [Column("TimeZone")]
    public short TimeZone { get; set; }

    [Column("VaultPassword")]
    public string VaultPassword { get; set; } = string.Empty;

    [Column("IsVaultExtended")]
    public bool IsVaultExtended { get; set; }

    [Column("IsBot")]
    public bool IsBot { get; set; }

    [Column("VipTier")]
    public short VipTier { get; set; }

    [Column("VipExpiry")]
    public DateTime? VipExpiry { get; set; }

    [Column("WheelSpins")]
    public int WheelSpins { get; set; }

    [Column("WCoin")]
    public int WCoin { get; set; }

    [Column("VaultId")]
    public Guid? VaultId { get; set; }

    public ItemStorage? Vault { get; set; }

    public List<Character> Characters { get; set; } = new();
}
