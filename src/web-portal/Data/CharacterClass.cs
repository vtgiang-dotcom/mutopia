using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("CharacterClass", Schema = "config")]
public class CharacterClass
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("Number")]
    public short Number { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("CanGetCreated")]
    public bool CanGetCreated { get; set; }
}
