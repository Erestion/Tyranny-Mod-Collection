using System;
using Game;

namespace AutoLootBepInEx
{
    internal static class LootEligibility
    {
        internal static bool HasCollectibleItems(Game.Container container, PlayerInventory inventory)
        {
            if (!container.HasInteracted)
            {
                return true;
            }
            Inventory component = container.GetComponent<Inventory>();
            if (!component)
            {
                return false;
            }
            foreach (InventoryItem inventoryItem in component.ItemList)
            {
                if (inventoryItem != null && inventoryItem.BaseItem && inventory.CanPutItem(inventoryItem))
                {
                    return true;
                }
            }
            return false;
        }
    }
}