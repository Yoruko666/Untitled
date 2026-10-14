using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Untitled.UI;

namespace Untitled.Editor
{
    public static partial class GoblinUIBuilder
    {
        [MenuItem("Untitled/UI/Open Extended Showcase")]
        public static void OpenExtended()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ExtendedScenePath)) EditorSceneManager.OpenScene(ExtendedScenePath); else BuildExtended();
        }
        [MenuItem("Untitled/UI/Generate Second Round Assets")]
        public static void BuildExtended()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ExtendedScenePath)) throw new InvalidOperationException("Extended Showcase exists; edit its saved assets directly.");
            PrepareExtendedAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrepareExtendedPrefabs(); EnsureShowcaseCamera();
            var root = new GameObject("Extended UI Showcase", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var theme = root.AddComponent<UIThemeContext>(); theme.Configure(day, night);
            var events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var backdrop = Image(root.transform, "Backdrop", null, 0, 0, 1920, 1080); Stretch(backdrop.rectTransform); backdrop.color = day.Background;
            var safe = Rect("SafeArea", root.transform, 0, 0, 1792, 952); Stretch(safe, 64, 64, 64, 64); safe.gameObject.AddComponent<UISafeArea>();
            var phase = Image(safe, "PhaseIcon", day.PhaseIcon, 0, 0, 60, 60);
            Text(safe, "Title", "哥布林奶茶店", 80, -2, 1100, 64, 46, UIVisualRole.Heading);
            Text(safe, "Tagline", "随身行囊，夜班闲话。", 80, 72, 1100, 36, 25, UIVisualRole.Muted);
            var themeButton = Button(safe, "ThemeButton", "切换到夜晚", UIButtonStyle.Primary, 1528, 8, 264, 56);
            Text(safe, "Edition", "UI 组件工坊 / 第二轮", 1340, 78, 452, 36, 24, UIVisualRole.Muted).alignment = TMPro.TextAlignmentOptions.TopRight;
            ColorImage(safe, "Rule", UIVisualRole.Border, 0, 132, 1792, 2);
            Text(safe, "Chapter", "02 / 背包、对话与奖励", 0, 160, 1100, 42, 24, UIVisualRole.Muted);
            var inventory = Instance<UIInventoryPanel>("UIInventoryPanel", safe, 0, 220);
            var tooltip = Instance<UIItemTooltip>("UIItemTooltip", safe, 860, 220);
            var dialogue = Instance<UIDialogueBox>("UIDialogueBox", safe, 860, 492);
            var inventoryButton = Button(safe, "ToggleInventory", "收起背包", UIButtonStyle.Secondary, 0, 866, 254, 56);
            var rewardButton = Button(safe, "OpenReward", "查看奖励", UIButtonStyle.Primary, 282, 866, 254, 56);
            var dialogueButton = Button(safe, "OpenDialogue", "重新对话", UIButtonStyle.Secondary, 860, 866, 254, 56);
            var refreshButton = Button(safe, "Refresh", "刷新背包数据", UIButtonStyle.Primary, 1142, 866, 254, 56);
            Text(safe, "Keys", "T 切换主题  /  B 背包", 1424, 872, 368, 40, 22, UIVisualRole.Muted);
            var feedback = Text(safe, "Feedback", "选择物品查看详情 · Enter / 空格确认 · 奖励弹窗中 Esc 关闭", 0, 934, 1792, 34, 22, UIVisualRole.Muted);
            var popupGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UIRewardPopup.prefab"), root.transform);
            popupGo.SetActive(false); var reward = popupGo.GetComponent<UIRewardPopup>();
            var mock = root.AddComponent<UIExtendedMockSource>(); mock.Configure(milkIcon, leafIcon, woodIcon, crystalIcon, pearlsIcon, bagIcon, goblinIcon, day.CoinIcon, day.MilkTeaIcon, day.QuestIcon);
            var presenter = root.AddComponent<UIInventoryPresenter>(); presenter.Configure(mock, inventory);
            var controller = root.AddComponent<UIExtendedShowcaseController>();
            controller.Theme = theme; controller.Mock = mock; controller.Inventory = inventory; controller.Tooltip = tooltip; controller.Dialogue = dialogue; controller.Reward = reward;
            controller.ThemeButton = themeButton; controller.RefreshButton = refreshButton; controller.InventoryButton = inventoryButton; controller.DialogueButton = dialogueButton; controller.RewardButton = rewardButton;
            controller.Feedback = feedback; controller.Backdrop = backdrop; controller.PhaseIcon = phase;
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, ExtendedScenePath);
            Debug.Log("GOBLIN_UI_SECOND_ROUND_BUILD_OK: 7 extended prefabs, existing dialogue address registered, extended scene.");
        }
    }
}
