using System;
using System.Collections.Generic;
using UnityEngine;
using Untitled.Config;

namespace Untitled.UI
{
    // Adapts gameplay inventory events to immutable display snapshots; never changes quantities.
    public sealed class UIInventoryDataSource : MonoBehaviour, IUIInventoryReadModel
    {
        public UIInventorySnapshot Current { get; private set; }
        public event Action<UIInventorySnapshot> Changed;
        private IReadOnlyList<ItemData> items;
        private ItemDatabase catalog;
        private Func<string, Sprite> resolveIcon;
        private string title;
        private InventoryData inventory;
        private bool listening;
        public void Initialize(IReadOnlyList<ItemData> inventory, ItemDatabase database, Func<string, Sprite> cachedIconResolver, string displayTitle = "背包")
        {
            Unsubscribe();
            this.inventory = null;
            items = inventory; catalog = database; resolveIcon = cachedIconResolver; title = displayTitle;
        }
        public void Initialize(InventoryData value, ItemDatabase database, Func<string, Sprite> cachedIconResolver, string displayTitle = "背包")
        {
            Initialize(value.Items, database, cachedIconResolver, displayTitle);
            inventory = value;
            if (isActiveAndEnabled) Subscribe();
            Publish();
        }
        private void OnEnable() { Subscribe(); if (inventory != null) Publish(); }
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();
        private void Subscribe() { if (inventory != null && !listening) { inventory.Changed += Publish; listening = true; } }
        private void Unsubscribe() { if (inventory != null && listening) inventory.Changed -= Publish; listening = false; }
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
