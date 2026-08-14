using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("ItemDefinition", Schema = "config")]
public class ItemDefinition
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("Group")]
    public byte Group { get; set; }

    [Column("Number")]
    public short Number { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("Width")]
    public byte Width { get; set; }

    [Column("Height")]
    public byte Height { get; set; }
}
