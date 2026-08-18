using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("GuildMember", Schema = "guild")]
public class GuildMember
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("GuildId")]
    public Guid GuildId { get; set; }

    [Column("Status")]
    public short Status { get; set; }

    public Guild? Guild { get; set; }
    public Character? Character { get; set; }
}
