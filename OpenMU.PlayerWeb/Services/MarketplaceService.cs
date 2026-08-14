using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

public class MarketplaceService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public MarketplaceService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<MarketplaceItem>> GetActiveListingsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.MarketplaceItems
            .Include(m => m.Item)
            .ThenInclude(i => i.Definition)
            .Include(m => m.Seller)
            .Where(m => m.Status == 0)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Item>> GetVaultItemsAsync(Guid accountId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var account = await db.Accounts
            .Include(a => a.Vault)
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account?.VaultId == null) return new List<Item>();

        return await db.Items
            .Include(i => i.Definition)
            .Where(i => i.ItemStorageId == account.VaultId)
            .ToListAsync();
    }

    public async Task<string?> ListItemAsync(Guid accountId, Guid itemId, int priceWCoin)
    {
        if (priceWCoin <= 0) return "Price must be greater than 0.";

        await using var db = await _dbFactory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
            if (account?.VaultId == null) return "Vault not found.";

            var item = await db.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.ItemStorageId == account.VaultId);
            if (item == null) return "Item not found in your vault.";

            // Move item to a new temporary storage for marketplace
            var holdingStorage = new ItemStorage { Id = Guid.NewGuid(), Money = 0 };
            db.ItemStorages.Add(holdingStorage);
            
            item.ItemStorageId = holdingStorage.Id;
            item.ItemSlot = 0; // Reset slot

            var listing = new MarketplaceItem
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                SellerAccountId = accountId,
                PriceWCoin = priceWCoin,
                CreatedAt = DateTime.UtcNow,
                Status = 0
            };

            db.MarketplaceItems.Add(listing);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return null;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return $"Error listing item: {ex.Message}";
        }
    }

    public async Task<string?> BuyItemAsync(Guid buyerAccountId, Guid marketplaceItemId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var listing = await db.MarketplaceItems
                .Include(m => m.Item)
                .FirstOrDefaultAsync(m => m.Id == marketplaceItemId && m.Status == 0);

            if (listing == null) return "Listing not found or already sold.";
            if (listing.SellerAccountId == buyerAccountId) return "You cannot buy your own item.";

            var buyer = await db.Accounts.FirstOrDefaultAsync(a => a.Id == buyerAccountId);
            if (buyer == null) return "Buyer account not found.";

            var seller = await db.Accounts.FirstOrDefaultAsync(a => a.Id == listing.SellerAccountId);
            if (seller == null) return "Seller account not found.";

            if (buyer.WCoin < listing.PriceWCoin) return "Not enough WCoin.";

            // Ensure buyer has a vault
            if (buyer.VaultId == null)
            {
                var newVault = new ItemStorage { Id = Guid.NewGuid(), Money = 0 };
                db.ItemStorages.Add(newVault);
                buyer.VaultId = newVault.Id;
                await db.SaveChangesAsync();
            }

            // Perform transaction
            buyer.WCoin -= listing.PriceWCoin;
            seller.WCoin += listing.PriceWCoin;

            // Move item to buyer's vault, placing it in the first free slot.
            if (buyer.VaultId is not { } buyerVaultId)
            {
                return "Buyer vault is missing.";
            }

            var definition = listing.Item.Definition;
            if (definition is null)
            {
                return "Item definition is missing.";
            }

            var oldStorageId = listing.Item.ItemStorageId;
            if (!await VaultItemPlacer.TryPlaceAsync(db, buyerVaultId, listing.Item, definition))
            {
                return "Buyer vault is full. Free up space and try again.";
            }

            listing.Item.ItemStorageId = buyerVaultId;
            listing.Status = 1; // Sold

            await db.SaveChangesAsync();
            
            // Clean up old holding storage
            if (oldStorageId != null)
            {
                var oldStorage = await db.ItemStorages.FindAsync(oldStorageId);
                if (oldStorage != null) db.ItemStorages.Remove(oldStorage);
                await db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return null;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return $"Error buying item: {ex.Message}";
        }
    }
}
