using System.Collections.Generic;
using UnityEngine;

namespace Untitled
{
    public interface IItemCatalog
    {
        bool TryGetStackLimit(int itemId, out int maxStack);
    }

    // Immutable session copy: playing never writes to the source table or SO assets.
    public sealed class ItemCatalog : IItemCatalog
    {
        private readonly Dictionary<int, int> stackLimits = new Dictionary<int, int>();

        public ItemCatalog(ItemDatabase database, InventorySettings settings = null)
        {
            if (database != null)
                foreach (int id in database.All.Keys)
                    if (id > 0) stackLimits.Add(id, int.MaxValue);
            if (settings == null) return;

            var configured = new HashSet<int>();
            foreach (var definition in settings.Items)
            {
                if (definition == null) continue;
                if (!stackLimits.ContainsKey(definition.ItemId))
                {
                    Debug.LogWarning($"[ItemCatalog] 堆叠配置引用未知物品 {definition.ItemId}，已跳过。");
                    continue;
                }
                if (!configured.Add(definition.ItemId))
                {
                    Debug.LogWarning($"[ItemCatalog] 物品 {definition.ItemId} 重复配置，保留第一条。");
                    continue;
                }
                stackLimits[definition.ItemId] = definition.MaxStack;
            }
        }

        public bool TryGetStackLimit(int itemId, out int maxStack) => stackLimits.TryGetValue(itemId, out maxStack);
    }
}
