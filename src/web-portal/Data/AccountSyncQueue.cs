using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("AccountSyncQueue", Schema = "data")]
public class AccountSyncQueue
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string HashedPasswordS16 { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Action { get; set; } = "create"; // 'create' | 'update_password'

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RetryCount { get; set; } = 0;

    public string? LastError { get; set; }
}