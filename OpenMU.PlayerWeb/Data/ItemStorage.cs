using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("ItemStorage", Schema = "data")]
public class ItemStorage
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("Money")]
    public int Money { get; set; }
}
