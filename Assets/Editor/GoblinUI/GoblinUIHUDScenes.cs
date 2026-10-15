using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Untitled.UI;

namespace Untitled.Editor
{
    public static partial class GoblinUIBuilder
    {
        public const string HUDScenePath = "Assets/Scenes/UI/UIHUDShowcase.unity";
        [MenuItem("Untitled/UI/Open HUD Showcase")]
        public static void OpenHUD()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(HUDScenePath);
        }
        public static void BuildHUD()
        {
            if (File.Exists(HUDScenePath)) throw new InvalidOperationException("HUD assets already authored; edit saved assets.");
            PrepareExtendedAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrepareHUDParts();
            foreach (bool isNight in new[] { false, true }) { var root = MakeHUD(isNight); Save(root, isNight ? "UI_NightHUD" : "UI_DayHUD"); }
            var demo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UI_DayHUD.prefab"));
            demo.GetComponent<UIGameplayReadModel>().enabled = false;
            var mock = demo.AddComponent<UIHUDMockSource>(); mock.HealthIcon = day.MilkTeaIcon; mock.ResourceIcon = crystalIcon; mock.ItemIcon = day.MilkTeaIcon;
            demo.GetComponent<UIGameplayHUD>().Source = mock;
            var background = Image(demo.transform, "DemoBackdrop", null, 0, 0, 1920, 1080); Stretch(background.rectTransform); background.rectTransform.SetAsFirstSibling(); background.color = day.Background;
            Text(demo.transform, "DemoCaption", "HUD SHOWCASE  ·  N 日夜 / L 警戒 / Esc 暂停 / 1–5 快捷栏", 64, 1020, 1700, 36, 20, UIVisualRole.Muted);
            var controls = demo.AddComponent<UIHUDDemoController>(); controls.Mock = mock; controls.Background = background; controls.Theme = demo.GetComponent<UIThemeContext>();
            new GameObject("UI Demo Bootstrap").AddComponent<Bootstrap>(); EnsureEvents(); EnsureShowcaseCamera();
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, HUDScenePath);
            IntegrateHUDScenes();
        }
        public static void IntegrateHUDScenes()
        {
            var demo = EditorSceneManager.OpenScene(HUDScenePath);
            EnsureEvents();
            EditorSceneManager.SaveScene(demo);
            IntegrateScene("Assets/Scenes/Gameplay/Bussiness.unity", false);
            IntegrateScene("Assets/Scenes/Gameplay/Adventure.unity", true);
            Debug.Log("GOBLIN_UI_HUD_BUILD_INTEGRATION_OK");
        }
        public static void RepairHUDVisuals()
        {
            foreach (string name in new[] { "UI_DayHUD", "UI_NightHUD" })
            {
                string path = Prefabs + "/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                var hud = root.GetComponent<UIGameplayHUD>();
                hud.PhaseIcon = root.transform.Find("SafeArea/Day/PhaseIcon").GetComponent<Image>();
                PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
            }
            var status = PrefabUtility.LoadPrefabContents(Prefabs + "/UIStatusBar.prefab");
            status.transform.Find("Track/Fill").GetComponent<Image>().preserveAspect = false;
            PrefabUtility.SaveAsPrefabAsset(status, Prefabs + "/UIStatusBar.prefab"); PrefabUtility.UnloadPrefabContents(status);
            var texture = NewTexture(32, 32);
            Func<int, int, bool> heart = (x, y) => y >= 5 && y <= 28 && (y < 19
                ? Mathf.Abs(x - 16) <= y - 4
                : (x - 10) * (x - 10) + (y - 22) * (y - 22) < 49 || (x - 22) * (x - 22) + (y - 22) * (y - 22) < 49);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                if (heart(x, y)) texture.SetPixel(x, y, !heart(x - 1, y) || !heart(x + 1, y) || !heart(x, y - 1) || !heart(x, y + 1) ? C("392529") : C("D96C5F"));
            Fill(texture, 7, 23, 4, 2, C("F6CAB5"));
            var icon = SaveSprite("heart_hud_placeholder", texture, Vector4.zero);
            var demo = EditorSceneManager.OpenScene(HUDScenePath);
            UnityEngine.Object.FindObjectOfType<UIHUDMockSource>().HealthIcon = icon;
            EditorSceneManager.SaveScene(demo); AssetDatabase.SaveAssets();
            Debug.Log("GOBLIN_UI_HUD_VISUAL_REPAIR_OK");
        }
        private static GameObject MakeHUD(bool isNight)
        {
            var root = new GameObject(isNight ? "UI_NightHUD" : "UI_DayHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var theme = root.AddComponent<UIThemeContext>(); theme.Configure(day, night, true); theme.Apply(isNight);
            var safe = Rect("SafeArea", root.transform, 0, 0, 1792, 952); Stretch(safe, 64, 64, 64, 64); safe.gameObject.AddComponent<UISafeArea>();
            var dayFrame = Frame(safe, "Day", 0, 0, 320, 64);
            var phaseIcon = Image(dayFrame.transform, "PhaseIcon", isNight ? night.PhaseIcon : day.PhaseIcon, 18, 14, 36, 36);
            var dayText = Text(dayFrame.transform, "DayNumber", isNight ? "夜晚 第 1 天" : "白天 第 1 天", 68, 14, 234, 38, 24, UIVisualRole.Heading);
            var money = Instance<UICurrencyDisplay>("UICurrencyDisplay", safe, 1456, 0);
            var pause = Button(safe, "Pause", "Ⅱ", UIButtonStyle.Secondary, 1728, 0, 64, 64);
            var target = Frame(safe, "OrderTarget", 0, 90, 440, 106);
            Text(target.transform, "Heading", "当前订单", 22, 12, 396, 32, 22, UIVisualRole.Heading);
            var order = Text(target.transform, "Order", "等待顾客下单", 22, 52, 396, 38, 24);
            var hp = Instance<UIStatusBar>("UIStatusBar", safe, 0, 100);
            var resource = Instance<UIStatusBar>("UIStatusBar", safe, 0, 180); resource.name = "UIResourceBar";
            var resourceConfig = new SerializedObject(resource); resourceConfig.FindProperty("danger").boolValue = false; resourceConfig.ApplyModifiedPropertiesWithoutUndo();
            var quests = Instance<UIQuestTracker>("UIHUDQuestTracker", safe, 0, 260);
            var quick = Rect("Quickbar", safe, 652, 840, 488, 80); var bar = quick.gameObject.AddComponent<UIQuickbar>(); bar.Slots = new UIItemSlot[5];
            for (int i = 0; i < 5; i++)
            {
                bar.Slots[i] = Instance<UIItemSlot>("UIQuickSlot", quick, i * 100, 0);
                bar.Slots[i].GetComponentsInChildren<TMP_Text>().Single(t => t.name == "Shortcut").text = (i + 1).ToString();
            }
            var prompt = Instance<UIInteractionPrompt>("UIInteractionPrompt", safe, 656, 540);
            var crosshair = Rect("Crosshair", safe, 889, 469, 14, 14);
            Image(crosshair, "H", null, 0, 6, 14, 2); Image(crosshair, "V", null, 6, 0, 2, 14);
            var alert = Frame(safe, "Alert", 1400, 96, 392, 82);
            Text(alert.transform, "AlertText", "警戒中！", 24, 18, 344, 48, 30, UIVisualRole.Danger);
            var inventory = root.AddComponent<UIInventoryDataSource>();
            var source = root.AddComponent<UIGameplayReadModel>(); source.Configure(isNight, inventory, fallback: bagIcon);
            root.AddComponent<UIWorldInputBridge>();
            var view = root.AddComponent<UIGameplayHUD>(); view.Source = source; view.Theme = theme; view.PhaseIcon = phaseIcon; view.Day = dayText; view.Order = order; view.OrderRoot = target.gameObject;
            view.Coins = money; view.Health = hp; view.Resource = resource; view.Quests = quests; view.Quickbar = bar; view.Prompt = prompt; view.Crosshair = crosshair.gameObject; view.Alert = alert.gameObject; view.PauseButton = pause;
            hp.SetVisible(false); resource.SetVisible(false); quests.SetVisible(false); quick.gameObject.SetActive(false); prompt.SetVisible(false); alert.gameObject.SetActive(false);
            crosshair.gameObject.SetActive(!isNight); target.gameObject.SetActive(!isNight);
            foreach (var themed in root.GetComponentsInChildren<UIThemedView>(true)) themed.ApplyTheme(isNight ? night : day);
            return root;
        }
        private static void EnsureEvents()
        {
            var events = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<EventSystem>(true)).SingleOrDefault();
            if (events == null)
            {
                var go = new GameObject("Goblin UI EventSystem"); go.SetActive(false);
                events = go.AddComponent<EventSystem>();
            }
            var module = events.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = events.gameObject.AddComponent<InputSystemUIInputModule>();
            module.UnassignActions();
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions");
            module.actionsAsset = asset;
            module.point = InputActionReference.Create(asset.FindAction("UI/Point", true));
            module.leftClick = InputActionReference.Create(asset.FindAction("UI/Click", true));
            module.rightClick = InputActionReference.Create(asset.FindAction("UI/RightClick", true));
            module.middleClick = InputActionReference.Create(asset.FindAction("UI/MiddleClick", true));
            module.scrollWheel = InputActionReference.Create(asset.FindAction("UI/ScrollWheel", true));
            module.move = InputActionReference.Create(asset.FindAction("UI/Navigate", true));
            module.submit = InputActionReference.Create(asset.FindAction("UI/Submit", true));
            module.cancel = InputActionReference.Create(asset.FindAction("UI/Cancel", true));
            events.gameObject.SetActive(true);
        }
        private static string CameraFingerprint()
        {
            return string.Join("\n", UnityEngine.Object.FindObjectsOfType<Camera>().OrderBy(c => c.name).Select(c => c.name + EditorJsonUtility.ToJson(c) + EditorJsonUtility.ToJson(c.transform)));
        }
        private static void IntegrateScene(string path, bool isNight)
        {
            var scene = EditorSceneManager.OpenScene(path); string before = CameraFingerprint();
            var existing = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<UIGameplayHUD>(true)).SingleOrDefault();
            var root = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + (isNight ? "/UI_NightHUD.prefab" : "/UI_DayHUD.prefab")));
            var source = root.GetComponent<UIGameplayReadModel>();
            var shop = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<ShopSceneBindings>(true)).SingleOrDefault();
            var ray = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<ShopRaycastInteractor>(true)).SingleOrDefault();
            source.Configure(isNight, root.GetComponent<UIInventoryDataSource>(), shop, ray, bagIcon);
            root.GetComponent<UIWorldInputBridge>().Configure(shop != null ? shop.Player : null);
            if (!scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Bootstrap>(true)).Any()) new GameObject("Goblin UI Session Bootstrap").AddComponent<Bootstrap>();
            EnsureEvents();
            var sceneRoot = scene.GetRootGameObjects().SingleOrDefault(go => go.name == "SceneRoot");
            if (sceneRoot != null)
            {
                var ui = sceneRoot.transform.Cast<Transform>().SingleOrDefault(t => t.name == "UI");
                if (ui == null)
                {
                    var go = new GameObject("UI");
                    Undo.RegisterCreatedObjectUndo(go, "Create scene UI group");
                    go.transform.SetParent(sceneRoot.transform, false);
                    ui = go.transform;
                }
                Undo.SetTransformParent(root.transform, ui, "Organize gameplay HUD");
                foreach (var events in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<EventSystem>(true)).ToArray())
                    if (events.transform.parent == null) Undo.SetTransformParent(events.transform, ui, "Organize UI events");
                var systems = sceneRoot.transform.Cast<Transform>().Single(t => t.name == "Systems");
                foreach (var bootstrap in scene.GetRootGameObjects().Where(go => go.GetComponent<Bootstrap>() != null).ToArray())
                    Undo.SetTransformParent(bootstrap.transform, systems, "Organize UI bootstrap");
            }
            if (CameraFingerprint() != before) throw new InvalidOperationException("UI integration changed existing cameras.");
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("GOBLIN_UI_CAMERA_PRESERVED " + path);
        }
    }
}
