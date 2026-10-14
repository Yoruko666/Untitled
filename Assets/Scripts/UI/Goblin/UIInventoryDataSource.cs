using System;
using System.Collections.Generic;
using UnityEngine;
using Untitled.Config;

namespace Untitled.UI
{
    // Inject the existing warehouse or exploration package; the host publishes after mutations.
    public sealed class UIInventoryDataSource : MonoBehaviour, IUIInventoryReadModel
    {
        public UIInventorySnapshot Current { get; private set; }
        public event Action<UIInventorySnapshot> Changed;
        private IReadOnlyList<ItemData> items;
        private ItemDatabase catalog;
        private Func<string, Sprite> resolveIcon;
        private string title;
        public void Initialize(IReadOnlyList<ItemData> inventory, ItemDatabase database, Func<string, Sprite> cachedIconResolver, string displayTitle = "背包")
        { items = inventory; catalog = database; resolveIcon = cachedIconResolver; title = displayTitle; }
        public void Publish()
        {
            if (items == null || catalog == null) throw new InvalidOperationException("Inject inventory and item catalog before publishing.");
            var display = new List<UIItemData>();
            foreach (var item in items)
            {
                if (item == null || item.Num <= 0) continue;
                var row = catalog.Get(item.Id);
                display.Add(new UIItemData(item.Id.ToString(), row?.Name ?? "未知物品 #" + item.Id,
                    row != null && row.Type == ItemType.Ingredient ? "材料" : "其他", row?.Description ?? "暂无道具描述。", item.Num,
                    row == null ? null : resolveIcon?.Invoke(row.Icon)));
            }
            Current = new UIInventorySnapshot(title, display); Changed?.Invoke(Current);
        }
    }
}
