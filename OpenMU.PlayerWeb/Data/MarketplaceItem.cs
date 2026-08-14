using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

[Table("OpenMuWeb_MarketplaceItem", Schema = "data")]
public class MarketplaceItem
{
    [Key]
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }
    
    public Guid SellerAccountId { get; set; }
    
    public int PriceWCoin { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 0 = Active, 1 = Sold, 2 = Cancelled
    /// </summary>
    public int Status { get; set; }

    public Item Item { get; set; } = null!;
    public Account Seller { get; set; } = null!;
}
