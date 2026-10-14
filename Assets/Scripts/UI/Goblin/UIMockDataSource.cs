using System;
using UnityEngine;

namespace Untitled.UI
{
    [AddComponentMenu("Untitled/UI/Demo/Mock Data Source")]
    public sealed class UIMockDataSource : MonoBehaviour, IUIReadModel
    {
        public event Action<UISnapshot> Changed;
        private UISnapshot current;
        private int sample;
        public UISnapshot Current => current ?? (current = CreateSample());
        public void NextSample()
        {
            sample = (sample + 1) % 3;
            current = CreateSample(); Changed?.Invoke(current);
        }
        private UISnapshot CreateSample()
        {
            var order = new UIOrderData("003", "红糖珍珠奶茶", "绿耳朵 · 少冰 / 加珍珠", "红茶 ×1     牛奶 ×1\n红糖 ×1     珍珠 ×2",
                sample == 2 ? "配料齐全 · 可以开工" : "正在备料 · " + (sample + 2) + "/4", (sample + 2) / 4f, sample == 2);
            var quests = new[]
            {
                new UIQuestData("深入哥布林王国", "找到王庭入口，别碰国王的奶茶。", "0 / 1", false),
                new UIQuestData("收集发光水晶", "给店里的招牌换一颗新灯泡。", (sample + 3) + " / 5", sample == 2),
                new UIQuestData("救出被困的哥布林", "他声称自己只是来送外卖的。", "1 / 3", false)
            };
            return new UISnapshot(1, 320 + sample * 20, order, quests);
        }
    }
}
