using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

/// <summary>
/// Represents a character from the OpenMU <c>data.Character</c> table.
/// </summary>
[Table("Character", Schema = "data")]
public class Character
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("CharacterClassId")]
    public Guid CharacterClassId { get; set; }

    [Column("AccountId")]
    public Guid? AccountId { get; set; }

    [Column("CurrentMapId")]
    public Guid? CurrentMapId { get; set; }

    [Column("InventoryId")]
    public Guid? InventoryId { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("CharacterSlot")]
    public short CharacterSlot { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }

    [Column("Experience")]
    public long Experience { get; set; }

    [Column("MasterExperience")]
    public long MasterExperience { get; set; }

    [Column("LevelUpPoints")]
    public int LevelUpPoints { get; set; }

    [Column("MasterLevelUpPoints")]
    public int MasterLevelUpPoints { get; set; }

    [Column("PositionX")]
    public short PositionX { get; set; }

    [Column("PositionY")]
    public short PositionY { get; set; }

    [Column("PlayerKillCount")]
    public int PlayerKillCount { get; set; }

    [Column("StateRemainingSeconds")]
    public int StateRemainingSeconds { get; set; }

    [Column("State")]
    public int State { get; set; }

    [Column("CharacterStatus")]
    public int CharacterStatus { get; set; }

    [Column("Pose")]
    public short Pose { get; set; }

    [Column("UsedFruitPoints")]
    public int UsedFruitPoints { get; set; }

    [Column("UsedNegFruitPoints")]
    public int UsedNegFruitPoints { get; set; }

    [Column("InventoryExtensions")]
    public int InventoryExtensions { get; set; }

    [Column("IsStoreOpened")]
    public bool IsStoreOpened { get; set; }

    [Column("StoreName")]
    public string? StoreName { get; set; }

    public Account? Account { get; set; }
    public CharacterClass? CharacterClass { get; set; }
    public GameMapDefinition? GameMapDefinition { get; set; }
    public ItemStorage? Inventory { get; set; }
}
