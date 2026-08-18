using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

public class ShopService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ShopService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<ShopItem>> GetShopItemsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ShopItems.Where(s => s.Stock > 0).ToListAsync();
    }

    public async Task<string?> BuyShopItemAsync(Guid accountId, Guid shopItemId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var shopItem = await db.ShopItems.FirstOrDefaultAsync(s => s.Id == shopItemId);
            if (shopItem == null || shopItem.Stock <= 0) return "Item is out of stock or does not exist.";

            var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
            if (account == null) return "Account not found.";

            if (account.WCoin < shopItem.PriceWCoin) return "Not enough WCoin.";

            // Find ItemDefinition
            var definition = await db.ItemDefinitions.FirstOrDefaultAsync(d => d.Group == shopItem.ItemGroup && d.Number == shopItem.ItemNumber);
            if (definition == null) return "Item definition not found in server config.";

            if (account.VaultId == null)
            {
                var newVault = new ItemStorage { Id = Guid.NewGuid(), Money = 0 };
                db.ItemStorages.Add(newVault);
                account.VaultId = newVault.Id;
                await db.SaveChangesAsync();
            }

            // Deduct WCoin and Stock
            account.WCoin -= shopItem.PriceWCoin;
            shopItem.Stock--;

            // Create new Item
            var newItem = new Item
            {
                Id = Guid.NewGuid(),
                DefinitionId = definition.Id,
                Durability = 255, // Max durability
                Level = shopItem.Level,
                HasSkill = false,
                SocketCount = 0,
                ItemSlot = 0,
                ItemStorageId = account.VaultId,
                PetExperience = 0,
            };

            if (account.VaultId is not { } vaultId)
            {
                return "Vault is missing.";
            }

            if (!await VaultItemPlacer.TryPlaceAsync(db, vaultId, newItem, definition))
            {
                return "Your vault is full. Free up space and try again.";
            }

            db.Items.Add(newItem);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return null;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return $"Error buying shop item: {ex.Message}";
        }
    }
}
