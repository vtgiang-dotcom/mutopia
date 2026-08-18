using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("Guild", Schema = "guild")]
public class Guild
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("Score")]
    public int Score { get; set; }

    [Column("Notice")]
    public string? Notice { get; set; }
}
