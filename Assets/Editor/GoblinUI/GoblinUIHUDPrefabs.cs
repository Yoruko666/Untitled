using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Untitled.UI;

namespace Untitled.Editor
{
    public static partial class GoblinUIBuilder
    {
        private static Image Frame(Transform parent, string name, float x, float y, float width, float height)
        {
            var image = Image(parent, name, day.PanelSprite, x, y, width, height);
            var theme = image.gameObject.AddComponent<UIThemeGraphic>(); theme.UsePanelSprite = true;
            return image;
        }
        private static void PrepareHUDParts()
        {
            var currency = Frame(null, "UICurrencyDisplay", 0, 0, 248, 64);
            var coin = Image(currency.transform, "Coin", day.CoinIcon, 18, 16, 32, 32);
            var amount = Text(currency.transform, "Amount", "0", 66, 15, 164, 38, 24, UIVisualRole.Heading);
            var money = currency.gameObject.AddComponent<UICurrencyDisplay>(); money.Configure(amount, coin); Save(currency.gameObject, "UICurrencyDisplay");
            var status = Frame(null, "UIStatusBar", 0, 0, 368, 68);
            var track = ColorImage(status.transform, "Track", UIVisualRole.Inset, 18, 48, 332, 6);
            var fill = Image(track.transform, "Fill", fillSprite, 0, 0, 332, 6); fill.preserveAspect = false; fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            var icon = Image(status.transform, "Icon", null, 18, 12, 28, 28);
            var label = Text(status.transform, "Value", "血量 120/120", 58, 11, 286, 32, 20);
            var bar = status.gameObject.AddComponent<UIStatusBar>(); bar.Configure(label, icon, fill, true); Save(status.gameObject, "UIStatusBar");
            var prompt = Frame(null, "UIInteractionPrompt", 0, 0, 480, 64);
            var key = Text(prompt.transform, "Key", "E", 18, 15, 36, 36, 24, UIVisualRole.Heading);
            var description = Text(prompt.transform, "Prompt", "交互", 70, 15, 388, 36, 24);
            var interaction = prompt.gameObject.AddComponent<UIInteractionPrompt>(); interaction.Configure(key, description); Save(prompt.gameObject, "UIInteractionPrompt");
            var quickRoot = Frame(null, "UIQuickSlot", 0, 0, 88, 80);
            var buttonControl = quickRoot.gameObject.AddComponent<Button>(); buttonControl.targetGraphic = quickRoot;
            UnityEngine.Object.DestroyImmediate(quickRoot.GetComponent<UIThemeGraphic>());
            var button = quickRoot.gameObject.AddComponent<UIButton>(); button.Configure(UIButtonStyle.Secondary, null, null, true);
            var itemIcon = Image(quickRoot.transform, "Icon", day.MilkTeaIcon, 16, 12, 36, 36);
            var count = Text(quickRoot.transform, "Count", "2", 56, 15, 20, 30, 18, UIVisualRole.Heading);
            Text(quickRoot.transform, "Shortcut", "1", 18, 48, 50, 30, 20, UIVisualRole.Muted);
            var slot = quickRoot.gameObject.AddComponent<UIItemSlot>(); slot.Configure(button, null, count, itemIcon, null); Save(quickRoot.gameObject, "UIQuickSlot");
            var row = Rect("UIHUDQuestRow", null, 0, 0, 388, 54); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 54;
            var questIcon = Image(row, "Icon", day.QuestIcon, 0, 10, 28, 28);
            var title = Text(row, "Title", "收集发光水晶", 44, 9, 260, 32, 22, UIVisualRole.Heading);
            var counter = Text(row, "Counter", "3/5", 310, 10, 72, 32, 20, UIVisualRole.Muted);
            var empty = Text(row, "Detail", "", 0, 0, 0, 0, 18); empty.gameObject.SetActive(false);
            var questRow = row.gameObject.AddComponent<UIQuestRow>(); questRow.Configure(title, empty, counter, questIcon); Save(row.gameObject, "UIHUDQuestRow");
            var tracker = Panel(null, "UIHUDQuestTracker", "当前任务", day.QuestIcon, 0, 0, 440, 244);
            ScrollArea(tracker.transform, "TaskViewport", 26, 96, 388, 124, out var rows);
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 12; layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false;
            tracker.gameObject.AddComponent<UIQuestTracker>().Configure(rows, AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UIHUDQuestRow.prefab").GetComponent<UIQuestRow>());
            Save(tracker.gameObject, "UIHUDQuestTracker");
            MakePause(); MakeWorkWindows();
        }
        private static void MakePause()
        {
            string path = AssetDatabase.GUIDToAssetPath("18242a2a660c21940b357d1d9e04ea2e");
            var root = PrefabUtility.LoadPrefabContents(path);
            foreach (Transform child in root.transform) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var rootRect = root.GetComponent<RectTransform>(); Stretch(rootRect);
            var backdrop = Image(root.transform, "Backdrop", null, 0, 0, 1920, 1080); Stretch(backdrop.rectTransform); backdrop.color = new Color(.03f, .04f, .05f, .85f); backdrop.raycastTarget = true;
            var frame = Panel(root.transform, "PauseFrame", "休息一下", day.PhaseIcon, 0, 0, 620, 520);
            var rect = frame.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero;
            Text(frame.transform, "Subtitle", "奶茶会凉，存档里的欠条不会。", 34, 106, 552, 44, 24, UIVisualRole.Muted);
            var resume = Button(frame.transform, "Resume", "继续游戏", UIButtonStyle.Primary, 34, 174, 552, 58);
            var help = Button(frame.transform, "HelpButton", "操作说明", UIButtonStyle.Secondary, 34, 246, 552, 58);
            var helpText = Text(frame.transform, "Help", "WASD 移动 · E 交互\nEsc 暂停 / 返回 · 1–5 快捷栏\nTab 选择 · Enter / 空格确认", 34, 340, 552, 128, 22);
            var view = root.AddComponent<UIPauseMenu>(); view.ResumeButton = resume; view.HelpButton = help; view.Help = helpText;
            var menu = root.GetComponent<MenuPanel>(); if (menu == null) menu = root.AddComponent<MenuPanel>(); menu.ConfigureView(view);
            var focus = root.AddComponent<UIModalFocus>(); focus.First = resume.Control;
            resume.Control.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = help.Control, selectOnUp = help.Control };
            help.Control.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = resume.Control, selectOnUp = resume.Control };
            PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
        }
        private static void MakeWorkWindows()
        {
            foreach (var kind in new[] { "Inventory", "Workstation" })
            {
                var root = Rect("UI_" + kind, null, 0, 0, 1920, 1080); Stretch(root);
                var blocker = Image(root, "Backdrop", null, 0, 0, 1920, 1080); Stretch(blocker.rectTransform); blocker.color = new Color(.025f, .035f, .05f, .75f); blocker.raycastTarget = true;
                string source = kind == "Inventory" ? "UIInventoryPanel" : "UIOrderCard";
                var body = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + source + ".prefab"), root);
                var rect = body.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero;
                var close = Button(body.transform, "Close", "×", UIButtonStyle.Secondary, rect.rect.width - 84, 22, 54, 48);
                if (kind == "Inventory") { var window = root.gameObject.AddComponent<UIInventoryWindow>(); window.View = body.GetComponent<UIInventoryPanel>(); window.CloseButton = close; }
                else { var window = root.gameObject.AddComponent<UIShopWorkPanel>(); window.View = body.GetComponent<UIOrderCard>(); window.CloseButton = close; }
                root.gameObject.AddComponent<UIModalFocus>().First = close.Control;
                Save(root.gameObject, "UI_" + kind);
                var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(Prefabs + "/UI_" + kind + ".prefab"), settings.FindGroup("Panels")); entry.address = "UI_" + kind;
            }
        }
    }
}
