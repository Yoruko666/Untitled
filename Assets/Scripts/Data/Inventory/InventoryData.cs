using System;
using System.Collections.Generic;

namespace Untitled
{
    public class InventoryData
    {
        private readonly List<ItemData> items = new List<ItemData>();
        private readonly Dictionary<int, ItemData> itemDict = new Dictionary<int, ItemData>();
        private readonly IItemCatalog catalog;

        public IReadOnlyList<ItemData> Items { get; }
        public int Capacity { get; }
        public long UsedSlots
        {
            get
            {
                long result = 0;
                foreach (var item in items) result += Slots(item.Num, StackLimit(item.Id));
                return result;
            }
        }
        public event Action Changed;

        // The parameterless overload preserves isolated legacy callers. Production injects a catalog.
        public InventoryData(IItemCatalog itemCatalog = null, int capacity = 0)
        {
            catalog = itemCatalog;
            Capacity = Math.Max(0, capacity);
            Items = items.AsReadOnly();
        }

        /// <summary>Legacy entry point; new gameplay should check TryAddItem's result.</summary>
        public void AddItem(int id, int num) => TryAddItem(id, num);

        /// <summary>All-or-nothing addition. Capacity counts stacks, UI may show totals per ID.</summary>
        public bool TryAddItem(int id, int num)
        {
            int limit = StackLimit(id);
            if (num <= 0 || limit <= 0) return false;
            int oldCount = GetItemCount(id);
            if ((long)oldCount + num > int.MaxValue) return false;
            int count = oldCount + num;
            long nextSlots = UsedSlots - Slots(oldCount, limit) + Slots(count, limit);
            if (Capacity > 0 && nextSlots > Capacity) return false;

            SetCount(id, count);
            Changed?.Invoke();
            return true;
        }

        public bool ConsumeItem(int id, int num)
        {
            if (num <= 0 || !itemDict.TryGetValue(id, out var item) || item.Num < num) return false;
            int count = item.Num - num;
            if (count == 0)
            {
                items.Remove(item);
                itemDict.Remove(id);
            }
            else item.SetCount(count);
            Changed?.Invoke();
            return true;
        }

        public int GetItemCount(int itemId) => itemDict.TryGetValue(itemId, out var item) ? item.Num : 0;
        public bool HasItem(int itemId, int num) => num > 0 && GetItemCount(itemId) >= num;

        public void Clear()
        {
            if (items.Count == 0) return;
            items.Clear();
            itemDict.Clear();
            Changed?.Invoke();
        }

        public List<ItemStackSaveData> Capture()
        {
            var result = new List<ItemStackSaveData>();
            foreach (var item in items) result.Add(new ItemStackSaveData { ItemId = item.Id, Count = item.Num });
            return result;
        }

        // Restore retains valid contents even if a later configuration lowers capacity.
        // Unknown IDs, non-positive quantities and overflowing duplicates are skipped.
        public int Restore(IEnumerable<ItemStackSaveData> savedItems)
        {
            int skipped = 0;
            items.Clear();
            itemDict.Clear();
            if (savedItems != null)
                foreach (var saved in savedItems)
                {
                    if (saved == null || saved.Count <= 0 || StackLimit(saved.ItemId) <= 0
                        || (long)GetItemCount(saved.ItemId) + saved.Count > int.MaxValue)
                    {
                        skipped++;
                        continue;
                    }
                    SetCount(saved.ItemId, GetItemCount(saved.ItemId) + saved.Count);
                }
            Changed?.Invoke();
            return skipped;
        }

        private void SetCount(int id, int count)
        {
            if (itemDict.TryGetValue(id, out var existing)) existing.SetCount(count);
            else
            {
                var item = new ItemData(id, count);
                items.Add(item);
                itemDict.Add(id, item);
            }
        }

        private int StackLimit(int id)
        {
            if (id <= 0) return 0;
            if (catalog == null) return int.MaxValue;
            return catalog.TryGetStackLimit(id, out int value) && value > 0 ? value : 0;
        }

        private static long Slots(int count, int limit) => count > 0 && limit > 0 ? ((long)count + limit - 1) / limit : 0;
    }
}
