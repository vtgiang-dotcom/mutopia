using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("StatAttribute", Schema = "data")]
public class StatAttribute
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("DefinitionId")]
    public Guid? DefinitionId { get; set; }

    [Column("CharacterId")]
    public Guid? CharacterId { get; set; }

    [Column("Value")]
    public float Value { get; set; }

    public Character? Character { get; set; }
}
