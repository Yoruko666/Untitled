using System;
using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIHUDMockSource : MonoBehaviour, IUIHUDReadModel
    {
        public Sprite HealthIcon, ResourceIcon, ItemIcon;
        private bool night, alert;
        private UIHUDData current;
        public event Action<UIHUDData> Changed;
        public UIHUDData Current => current ?? (current = Sample());
        public void TogglePhase() { night = !night; Publish(); }
        public void ToggleAlert() { alert = !alert; Publish(); }
        private void Publish() { current = Sample(); Changed?.Invoke(current); }
        private UIHUDData Sample() => new UIHUDData(3, 320, night,
            new UIOrderData("003", "红糖珍珠奶茶 · 少冰", "绿耳朵", "红茶 / 牛奶 / 红糖 / 珍珠", "备料中", .5f, false),
            new[] { new UIQuestData("收集发光水晶", "", "3/5", false), new UIQuestData("寻找王庭入口", "", "0/1", false) },
            new UIStatusData("血量", 120, 120, HealthIcon), new UIStatusData("体力", 80, 100, ResourceIcon),
            new[] { new UIItemData("tea", "奶茶", "", "", 2, ItemIcon), new UIItemData("crystal", "水晶", "", "", 5, ResourceIcon) }, "查看制作台", alert);
    }
}
