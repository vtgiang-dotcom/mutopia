using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("OpenMuWeb_WheelSpin", Schema = "data")]
public class WheelSpin
{
    [Key]
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid CharacterId { get; set; }
    public DateTime SpunAt { get; set; }
    public string Prize { get; set; } = string.Empty;
    public short PrizeTier { get; set; }
}
