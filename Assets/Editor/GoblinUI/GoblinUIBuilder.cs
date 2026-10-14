using System;
using System.IO;
using TMPro;
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
        [MenuItem("Untitled/UI/Open Showcase")]
        public static void OpenShowcase()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Build();
        }
        [MenuItem("Untitled/UI/Generate First Round Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Showcase already exists; edit the saved prefabs/scene. Generation will not overwrite them.");
            PrepareAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PreparePrefabs();
            BuildShowcase();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("GOBLIN_UI_BUILD_OK: DayTheme, NightTheme, 7 prefabs and editable UIShowcase.");
            GoblinUIValidation.Validate();
        }
        private static T Instance<T>(string prefab, Transform parent, float x, float y) where T : Component
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + prefab + ".prefab"), parent);
            var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(x, -y);
            return go.GetComponent<T>();
        }
        internal static void EnsureShowcaseCamera()
        {
            if (UnityEngine.Object.FindObjectOfType<Camera>() != null) return;
            var camera = new GameObject("Showcase Camera", typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            camera.orthographic = true;
        }
        private static void BuildShowcase()
        {
            EnsureShowcaseCamera();
            var canvasGo = new GameObject("Goblin UI Showcase", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            var context = canvasGo.AddComponent<UIThemeContext>(); context.Configure(day, night);
            var eventSystem = new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var background = Image(canvas.transform, "Backdrop", null, 0, 0, 1920, 1080); Stretch(background.rectTransform); background.color = day.Background;
            var safe = Rect("SafeArea", canvas.transform, 0, 0, 1792, 952); Stretch(safe, 64, 64, 64, 64);
            safe.gameObject.AddComponent<UISafeArea>();
            // Every inscription, icon and frame is an independent editable object.
            var phase = Image(safe, "PhaseIcon", day.PhaseIcon, 0, 0, 60, 60);
            Text(safe, "Title", "哥布林奶茶店", 80, -2, 800, 64, 46, UIVisualRole.Heading);
            Text(safe, "Tagline", "一杯奶茶，连接两个世界。", 82, 70, 900, 36, 25, UIVisualRole.Muted);
            var edition = Text(safe, "Edition", "UI 组件工坊 / 第一轮", 1392, 8, 400, 40, 26, UIVisualRole.Heading); edition.alignment = TextAlignmentOptions.TopRight;
            var dayText = Text(safe, "Day", "第 1 天 / 七日物语", 1392, 57, 260, 34, 22, UIVisualRole.Muted);
            var coin = Image(safe, "CoinIcon", day.CoinIcon, 1656, 54, 32, 32);
            var coinText = Text(safe, "Coins", "320", 1700, 54, 92, 34, 24, UIVisualRole.Heading); coinText.alignment = TextAlignmentOptions.TopRight;
            ColorImage(safe, "TopRule", UIVisualRole.Border, 0, 132, 1792, 2);
            Text(safe, "Chapter", "01 / 可复用组件", 0, 160, 720, 42, 24, UIVisualRole.Muted);
            Text(safe, "Scope", "木框 · 羊皮纸 · 像素图标", 1168, 160, 624, 42, 24, UIVisualRole.Muted).alignment = TextAlignmentOptions.TopRight;

            var tools = Panel(safe, "Workshop", "交互试验台", day.PhaseIcon, 0, 220, 340, 612);
            Text(tools.transform, "ButtonsCaption", "按钮样式", 34, 105, 272, 34, 23, UIVisualRole.Muted);
            var primary = Instance<UIButton>("UIButton_Primary", tools.transform, 36, 151); primary.SetLabel("切换到夜晚");
            var secondary = Instance<UIButton>("UIButton_Secondary", tools.transform, 36, 225); secondary.SetLabel("次要按钮");
            var danger = Instance<UIButton>("UIButton_Danger", tools.transform, 36, 299); danger.SetLabel("危险按钮");
            var disabled = Instance<UIButton>("UIButton_Primary", tools.transform, 36, 373); disabled.SetLabel("不可用状态"); disabled.SetInteractable(false);
            Text(tools.transform, "KeyboardCaption", "键盘操作", 34, 464, 272, 34, 23, UIVisualRole.Heading);
            Text(tools.transform, "Keys", "Tab / 方向键 选择\nEnter / 空格 确认", 34, 507, 272, 72, 21, UIVisualRole.Muted);

            var order = Instance<UIOrderCard>("UIOrderCard", safe, 372, 220);
            var quests = Instance<UIQuestTracker>("UIQuestTracker", safe, 964, 220);
            // Quest panel keeps its native size. Right-hand notes occupy the remaining safe width.
            var notes = Panel(safe, "DesignNotes", "日夜色谱", day.CoinIcon, 1556, 220, 236, 480);
            Text(notes.transform, "PaletteCaption", "主题色", 26, 110, 184, 30, 22, UIVisualRole.Muted);
            var roles = new[] { UIVisualRole.Surface, UIVisualRole.Accent, UIVisualRole.Border, UIVisualRole.Danger };
            var labels = new[] { "面板", "强调", "边框", "危险" };
            for (int i = 0; i < roles.Length; i++)
            {
                ColorImage(notes.transform, "Swatch_" + i, roles[i], 26, 157 + i * 57, 32, 32);
                Text(notes.transform, "SwatchLabel_" + i, labels[i], 76, 157 + i * 57, 134, 32, 22);
            }
            Text(notes.transform, "PixelHint", "32px 图标\n9-slice 木框", 26, 401, 184, 54, 19, UIVisualRole.Muted);
            Text(safe, "ShopNoteText", "“珍珠要煮熟。哥布林的借条，也一样。”", 964, 752, 828, 64, 28, UIVisualRole.Heading);
            var visibility = Button(safe, "ToggleOrder", "收起订单", UIButtonStyle.Secondary, 372, 858, 266, 56);
            var refresh = Button(safe, "RefreshData", "刷新演示数据", UIButtonStyle.Primary, 666, 858, 266, 56);
            var feedback = Text(safe, "Feedback", "T 切换主题  /  R 刷新数据  /  O 打开或收起订单", 964, 858, 828, 62, 22, UIVisualRole.Muted);
            Text(safe, "Footer", "GOBLIN MILK TEA  ·  UI SHOWCASE", 0, 934, 700, 28, 18, UIVisualRole.Muted);
            Text(safe, "FooterRight", "可编辑素材 / 占位美术", 1292, 934, 500, 28, 18, UIVisualRole.Muted).alignment = TextAlignmentOptions.TopRight;
            var mock = canvasGo.AddComponent<UIMockDataSource>();
            var presenter = canvasGo.AddComponent<UIHUDPresenter>(); presenter.Configure(mock, order, quests, dayText, coinText);
            var controller = canvasGo.AddComponent<UIShowcaseController>();
            controller.Theme = context; controller.Mock = mock; controller.OrderPanel = order.GetComponent<Untitled.UI.UIPanel>();
            controller.ThemeButton = primary; controller.DataButton = refresh; controller.VisibilityButton = visibility;
            controller.SecondaryButton = secondary; controller.DangerButton = danger;
            controller.QuestScroll = quests.GetComponent<ScrollRect>();
            controller.Feedback = feedback; controller.Backdrop = background; controller.PhaseIcon = phase;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(order.ActionRequested, controller.OnOrderRequested);
        }
    }
}
