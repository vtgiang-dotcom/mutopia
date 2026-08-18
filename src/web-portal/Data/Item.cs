using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("Item", Schema = "data")]
public class Item
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("DefinitionId")]
    public Guid DefinitionId { get; set; }

    [Column("Durability")]
    public double Durability { get; set; }

    [Column("Level")]
    public byte Level { get; set; }

    [Column("HasSkill")]
    public bool HasSkill { get; set; }

    [Column("SocketCount")]
    public int SocketCount { get; set; }

    [Column("StorePrice")]
    public int? StorePrice { get; set; }

    [Column("ItemSlot")]
    public byte ItemSlot { get; set; }

    [Column("ItemStorageId")]
    public Guid? ItemStorageId { get; set; }

    [Column("PetExperience")]
    public int PetExperience { get; set; }

    public ItemDefinition Definition { get; set; } = null!;
}
