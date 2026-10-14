using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Untitled.UI;

namespace Untitled.Editor
{
    public static partial class GoblinUIBuilder
    {
        private static ScrollRect ScrollArea(Transform parent, string name, float x, float y, float width, float height, out RectTransform content)
        {
            var viewport = Rect(name, parent, x, y, width, height);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            content = Rect("ScrollContent", viewport, 0, 0, width, 0);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.sizeDelta = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            viewport.gameObject.AddComponent<UIScrollFollowSelection>().Configure(scroll);
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            return scroll;
        }
        private static TMP_Text ScrollText(Transform parent, string name, float x, float y, float width, float height, out ScrollRect scroll)
        {
            scroll = ScrollArea(parent, name, x, y, width, height, out var root);
            var body = root.gameObject.AddComponent<TextMeshProUGUI>(); body.font = font; body.fontSize = 24;
            body.richText = false; body.enableWordWrapping = true; body.overflowMode = TextOverflowModes.Overflow; body.raycastTarget = false;
            body.gameObject.AddComponent<UIThemeGraphic>();
            return body;
        }
        private static void PrepareExtendedPrefabs()
        {
            var slotRoot = Rect("UIItemSlot", null, 0, 0, 172, 104);
            var background = slotRoot.gameObject.AddComponent<Image>(); background.sprite = day.PanelSprite; background.type = UnityEngine.UI.Image.Type.Sliced;
            var buttonControl = slotRoot.gameObject.AddComponent<UnityEngine.UI.Button>(); buttonControl.targetGraphic = background;
            var button = slotRoot.gameObject.AddComponent<UIButton>(); button.Configure(UIButtonStyle.Secondary, null, null, true); button.ApplyTheme(day);
            var icon = Image(slotRoot, "ItemIcon", crystalIcon, 20, 14, 44, 44);
            var name = Text(slotRoot, "ItemName", "发光水晶", 16, 66, 140, 32, 20);
            var count = Text(slotRoot, "Quantity", "5", 82, 23, 72, 32, 22, UIVisualRole.Heading); count.alignment = TextAlignmentOptions.TopRight;
            var mark = ColorImage(slotRoot, "SelectedMark", UIVisualRole.Accent, 11, 18, 3, 64); mark.gameObject.SetActive(false);
            var slot = slotRoot.gameObject.AddComponent<UIItemSlot>(); slot.Configure(button, name, count, icon, mark.gameObject);
            Save(slotRoot.gameObject, "UIItemSlot");
            MakeInventory(); MakeTooltip(); MakeDialogue(); MakeReward();
        }
        private static void MakeInventory()
        {
            var panel = Panel(null, "UIInventoryPanel", "随身背包", bagIcon, 0, 0, 820, 620);
            var all = Button(panel.transform, "AllTab", "全部", UIButtonStyle.Secondary, 30, 108, 230, 54);
            var material = Button(panel.transform, "MaterialTab", "材料", UIButtonStyle.Secondary, 278, 108, 230, 54);
            var quest = Button(panel.transform, "QuestTab", "任务", UIButtonStyle.Secondary, 526, 108, 230, 54);
            var scroll = ScrollArea(panel.transform, "ItemViewport", 30, 180, 760, 344, out var grid);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>(); layout.cellSize = new Vector2(172, 104); layout.spacing = new Vector2(16, 16);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 4;
            var status = Text(panel.transform, "Status", "8 种物品 / 全部", 30, 534, 760, 34, 22, UIVisualRole.Muted);
            Text(panel.transform, "Hint", "滚轮 / PgUp PgDn 查看更多 · 数量由物品系统提供", 30, 576, 760, 28, 18, UIVisualRole.Muted);
            var view = panel.gameObject.AddComponent<UIInventoryPanel>();
            view.Configure(panel, grid, AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UIItemSlot.prefab").GetComponent<UIItemSlot>(), scroll, status, all, material, quest);
            Save(panel.gameObject, "UIInventoryPanel");
        }
        private static void MakeTooltip()
        {
            var panel = Panel(null, "UIItemTooltip", "发光水晶", crystalIcon, 0, 0, 932, 240);
            var category = Text(panel.transform, "Category", "材料 / 数量 5", 30, 100, 872, 34, 22, UIVisualRole.Muted);
            var body = ScrollText(panel.transform, "DescriptionViewport", 30, 146, 872, 68, out var scroll);
            body.text = "在黑暗中发出柔和蓝光，可用于修理招牌。";
            var tooltip = panel.gameObject.AddComponent<UIItemTooltip>(); tooltip.Configure(panel, category, body, scroll);
            Save(panel.gameObject, "UIItemTooltip");
        }
        private static void MakeDialogue()
        {
            var choice = Button(null, "UIDialogueChoice", "金币支付，先款后茶", UIButtonStyle.Secondary, 0, 0, 746, 50);
            choice.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;
            Save(choice.gameObject, "UIDialogueChoice");
            var panel = Panel(null, "UIDialogueBox", "绿耳朵 · 夜班顾客", goblinIcon, 0, 0, 932, 348);
            var portrait = Image(panel.transform, "Portrait", goblinIcon, 30, 106, 100, 100);
            Text(panel.transform, "PortraitCaption", "哥布林\n顾客", 30, 230, 100, 70, 20, UIVisualRole.Muted);
            var body = ScrollText(panel.transform, "LineViewport", 150, 100, 746, 88, out var lineScroll);
            body.text = "老板，来一杯红糖珍珠奶茶！";
            var optionsScroll = ScrollArea(panel.transform, "ChoicesViewport", 150, 212, 746, 110, out var choicesRoot);
            var layout = choicesRoot.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 10;
            layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var next = Button(panel.transform, "Continue", "继续", UIButtonStyle.Primary, 150, 264, 746, 54);
            var close = Button(panel.transform, "Close", "×", UIButtonStyle.Secondary, 848, 22, 54, 48);
            var dialogue = panel.gameObject.AddComponent<UIDialogueBox>();
            dialogue.Configure(panel, body, portrait, choicesRoot, AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UIDialogueChoice.prefab").GetComponent<UIButton>(), next, close, lineScroll, optionsScroll);
            Save(panel.gameObject, "UIDialogueBox");
            var formal = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UIDialogueBox.prefab"));
            formal.name = "UI_Dialogue";
            var rect = formal.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero;
            formal.AddComponent<DialoguePanel>().ConfigureView(formal.GetComponent<UIDialogueBox>());
            Save(formal, "UI_Dialogue");
            RegisterDialogue();
        }
        private static void MakeReward()
        {
            var root = Rect("UIRewardPopup", null, 0, 0, 1920, 1080); Stretch(root);
            var blocker = Image(root, "ModalBackdrop", null, 0, 0, 1920, 1080); Stretch(blocker.rectTransform);
            blocker.color = new Color(.025f, .035f, .055f, .86f); blocker.raycastTarget = true;
            var panel = Panel(root, "RewardFrame", "获得夜班奖励", day.CoinIcon, 0, 0, 740, 520);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero;
            var message = Text(panel.transform, "Message", "奖励已由演示数据提供 · 确认仅关闭展示", 36, 104, 668, 38, 22, UIVisualRole.Muted);
            var rewardScroll = ScrollArea(panel.transform, "RewardViewport", 36, 164, 668, 238, out var grid);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>(); layout.cellSize = new Vector2(196, 104); layout.spacing = new Vector2(16, 16);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 3;
            var confirm = Button(panel.transform, "Confirm", "确认已阅", UIButtonStyle.Primary, 36, 436, 668, 58);
            var close = Button(panel.transform, "Close", "×", UIButtonStyle.Secondary, 654, 22, 54, 48);
            var navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = close.Control, selectOnDown = close.Control, selectOnLeft = close.Control, selectOnRight = close.Control };
            confirm.Control.navigation = navigation;
            close.Control.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = confirm.Control, selectOnDown = confirm.Control, selectOnLeft = confirm.Control, selectOnRight = confirm.Control };
            var popup = root.gameObject.AddComponent<UIRewardPopup>();
            popup.Configure(panel, message, grid, AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UIItemSlot.prefab").GetComponent<UIItemSlot>(), confirm, close, rewardScroll);
            Save(root.gameObject, "UIRewardPopup");
        }
    }
}
