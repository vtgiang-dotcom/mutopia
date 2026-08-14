using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

/// <summary>
/// Shared helpers for placing items into a vault without colliding with items already stored there.
/// </summary>
public static class VaultItemPlacer
{
    /// <summary>
    /// The number of vault slots (a regular vault is an 8x8 grid). Extended vaults reuse the same
    /// storage with a larger window, so this stays at the base grid; items are 1x1 unless their
    /// definition says otherwise.
    /// </summary>
    public const int VaultSlotCount = 64;

    /// <summary>
    /// Finds the first free slot in the given storage for an item of the given width and height, and
    /// assigns it to <paramref name="item"/>. Returns false when the vault is full. A slot is occupied
    /// when any stored item's rectangle (anchor slot + width x height) covers it.
    /// </summary>
    public static async ValueTask<bool> TryPlaceAsync(AppDbContext db, Guid storageId, Item item, ItemDefinition definition, CancellationToken cancellationToken = default)
    {
        var width = definition.Width > 0 ? definition.Width : 1;
        var height = definition.Height > 0 ? definition.Height : 1;

        var occupied = await db.Items
            .Where(i => i.ItemStorageId == storageId)
            .Select(i => new { i.ItemSlot, Width = i.Definition!.Width, Height = i.Definition!.Height })
            .ToListAsync(cancellationToken);

        var grid = new bool[VaultSlotCount];
        foreach (var entry in occupied)
        {
            var w = entry.Width > 0 ? entry.Width : 1;
            var h = entry.Height > 0 ? entry.Height : 1;
            var row = entry.ItemSlot / 8;
            var col = entry.ItemSlot % 8;
            for (var r = row; r < row + h; r++)
            {
                for (var c = col; c < col + w; c++)
                {
                    if (r >= 8 || c >= 8)
                    {
                        continue;
                    }

                    grid[r * 8 + c] = true;
                }
            }
        }

        // A vault item's slot is its top-left cell. Find the first 1x1..NxM rectangle that fits.
        for (var slot = 0; slot < VaultSlotCount; slot++)
        {
            var row = slot / 8;
            var col = slot % 8;
            if (row + height > 8 || col + width > 8)
            {
                continue;
            }

            var fits = true;
            for (var r = row; r < row + height && fits; r++)
            {
                for (var c = col; c < col + width; c++)
                {
                    if (grid[r * 8 + c])
                    {
                        fits = false;
                        break;
                    }
                }
            }

            if (fits)
            {
                item.ItemSlot = (byte)slot;
                return true;
            }
        }

        return false;
    }
}
