using System;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIExtendedMockSource : MonoBehaviour, IUIInventoryReadModel
    {
        [SerializeField] private Sprite milk, leaf, wood, crystal, pearls, bag, goblin, coin, tea, recipe;
        private int sample;
        private UIInventorySnapshot current;
        public event Action<UIInventorySnapshot> Changed;
        public UIInventorySnapshot Current => current ?? (current = MakeInventory());
        public void Configure(Sprite milkIcon, Sprite leafIcon, Sprite woodIcon, Sprite crystalIcon, Sprite pearlsIcon, Sprite bagIcon, Sprite portrait, Sprite coinIcon, Sprite teaIcon, Sprite recipeIcon)
        { milk = milkIcon; leaf = leafIcon; wood = woodIcon; crystal = crystalIcon; pearls = pearlsIcon; bag = bagIcon; goblin = portrait; coin = coinIcon; tea = teaIcon; recipe = recipeIcon; }
        public void NextSample() { sample = (sample + 1) % 3; current = MakeInventory(); Changed?.Invoke(current); }
        private UIInventorySnapshot MakeInventory()
        {
            var items = new List<UIItemData>
            {
                new UIItemData("milk", "牛奶", "材料", "新鲜牛奶。标签写着来自快乐的奶牛；奶牛拒绝接受采访。", 12 + sample, milk),
                new UIItemData("leaf", "茶叶", "材料", "奶茶店的基础茶叶，泡久了连哥布林都觉得苦。", 8, leaf),
                new UIItemData("wood", "红糖", "材料", "切成小块的红糖。用于奶茶，也用于贿赂夜班店长。", 15, wood),
                new UIItemData("pearl", "珍珠", "材料", "圆滚滚的黑糖珍珠。请不要与地牢里的眼珠混用。", 6, pearls),
                new UIItemData("crystal", "发光水晶", "材料", "在黑暗中发出柔和蓝光，可用于修理招牌。它偶尔会抱怨加班。", 5 + sample, crystal),
                new UIItemData("bag", "旧钱袋", "其他", "里面没有金币，只有一张写着“明天一定还”的欠条。", 1, bag),
                new UIItemData("pass", "王庭通行证", "任务", "哥布林王国的通行证。背面小字：本证不包含珍珠加料。", 1, bag, false),
                new UIItemData("tea", "试喝样品", "材料", "给顾客准备的奶茶样品。少冰，别少良心。", 3, tea)
            };
            if (sample == 2) for (int i = 0; i < 12; i++) items.Add(new UIItemData("extra" + i, "备用茶叶", "材料", "额外收集的茶叶。", i + 1, leaf));
            return new UIInventorySnapshot("随身背包", items);
        }
        public UIDialogueData Greeting => new UIDialogueData("绿耳朵 · 夜班顾客", "老板，来一杯红糖珍珠奶茶！少冰，多加珍珠。\n如果我用王庭的欠条付款，能不能免掉加料费？", goblin,
            new[] { new UIDialogueChoice("pay", "金币支付，先款后茶"), new UIDialogueChoice("credit", "接受欠条（今日停用）", false) });
        public UIDialogueData Reply => new UIDialogueData("绿耳朵 · 夜班顾客", "好吧，金币在这里。至于欠条……\n国王说，他的信誉和王冠一样闪亮。我怀疑王冠也是借来的。", goblin);
        public UIRewardData Reward => new UIRewardData("demo-reward", "获得夜班奖励", "奖励已由演示数据提供 · 确认仅关闭展示", new[]
        {
            new UIItemData("coin", "金币", "奖励", "夜班辛苦费。", 200, coin, false),
            new UIItemData("crystal", "发光水晶", "奖励", "修理招牌所需的材料。", 3, crystal, false),
            new UIItemData("tea", "配方卷轴", "奖励", "新的奶茶配方。", 1, recipe, false)
        });
    }
}
