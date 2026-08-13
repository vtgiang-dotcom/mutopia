using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("GameMapDefinition", Schema = "config")]
public class GameMapDefinition
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("Number")]
    public short Number { get; set; }

    [Column("Name")]
    public string? Name { get; set; }
}
