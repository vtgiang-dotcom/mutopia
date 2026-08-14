using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("OpenMuWeb_ShopItem", Schema = "config")]
public class ShopItem
{
    [Key]
    public Guid Id { get; set; }

    public byte ItemGroup { get; set; }
    
    public short ItemNumber { get; set; }
    
    public byte Level { get; set; }
    
    public string OptionName { get; set; } = string.Empty;
    
    public int PriceWCoin { get; set; }
    
    public int Stock { get; set; }
}
