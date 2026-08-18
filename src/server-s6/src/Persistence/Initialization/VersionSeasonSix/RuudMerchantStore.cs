// <copyright file="RuudMerchantStore.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.Initialization.Items;

/// <summary>
/// Merchant store initialization for Priest James (Ruud Shop NPC).
/// </summary>
internal partial class NpcInitialization
{
    /// <summary>
    /// Creates the ItemStorage for Priest James (Ruud Shop Merchant).
    /// </summary>
    /// <param name="number">The NPC number.</param>
    /// <returns>The merchant <see cref="ItemStorage"/>.</returns>
    protected virtual ItemStorage CreateRuudMerchantItemStorage(short number)
    {
        List<Item> itemList = new()
        {
            // Mastery Skill Scrolls & Equipment Seeds
            this.ItemHelper.CreatePotion(0, 0, 1, 0),   // Base Seed Item
            this.ItemHelper.CreatePotion(1, 1, 1, 0),   // Mastery Essence
        };

        var merchantStore = this.Context.CreateNew<ItemStorage>();
        merchantStore.SetGuid(number);

        byte slot = 0;
        foreach (var item in itemList)
        {
            item.ItemSlot = slot++;
            merchantStore.Items.Add(item);
        }

        return merchantStore;
    }
}
