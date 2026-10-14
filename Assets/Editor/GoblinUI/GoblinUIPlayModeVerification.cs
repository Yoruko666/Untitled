using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Untitled.UI;

namespace Untitled.Editor
{
    // An opt-in, task-scoped check. It adds virtual input devices only for the verification run.
    [InitializeOnLoad]
    public static class GoblinUIPlayModeVerification
    {
        private const string Pending = "GoblinUI.VerificationPending";
        static GoblinUIPlayModeVerification() => EditorApplication.playModeStateChanged += OnPlayMode;
        public static void RunKeyboardOnly()
        {
            SessionState.SetBool("GoblinUI.KeyboardCheck", true);
            RunLayoutOnly();
        }
        public static void RunLayoutOnly()
        {
            SessionState.SetBool("GoblinUI.LayoutOnly", true);
            Run();
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(GoblinUIBuilder.ScenePath);
            GoblinUIValidation.Validate();
            SetResolution(1920, 1080);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            new GameObject("UI Verification (temporary)").AddComponent<GoblinUIVerificationRunner>();
        }
        public static void SetResolution(int width, int height)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var instance = sizesType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { Enum.Parse(groupType, "Standalone") });
            var sizeType = assembly.GetType("UnityEditor.GameViewSizeType");
            var size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), Enum.Parse(sizeType, "FixedResolution"), width, height, "UI Verification " + width + "x" + height);
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var gameViewType = assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(window, count - 1);
            window.Focus();
        }
    }

    public sealed class GoblinUIVerificationRunner : MonoBehaviour
    {
        private Mouse mouse;
        private Keyboard keyboard;
        private bool failed;
        private int errors;
        private int screenshots;
        private bool inputPassed;
        private string output;
        private IEnumerator Start()
        {
            output = Path.GetFullPath("docs/images/ui"); Directory.CreateDirectory(output);
            Application.logMessageReceived += OnLog;
            mouse = InputSystem.AddDevice<Mouse>("UI Verification Mouse");
            keyboard = InputSystem.AddDevice<Keyboard>("UI Verification Keyboard");
            var checks = Checks();
            while (true)
            {
                object current;
                try { if (!checks.MoveNext()) break; current = checks.Current; }
                catch (Exception exception) { Debug.LogException(exception); failed = true; break; }
                yield return current;
            }
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            Application.logMessageReceived -= OnLog;
            var result = failed || errors > 0 ? "FAILED" : "PASSED";
            File.WriteAllText(output + "/verification.txt", "Unity 2022.3.62f3c1 Editor Play Mode: " + result + "\n" +
                "Input / data / lifecycle checks: " + (inputPassed ? "PASSED" : "incomplete") + "\nLayout checks at 1920x1080 and 1280x720: " + (screenshots == 4 ? "PASSED" : "incomplete") + "\nScreenshots: " + screenshots + "\nErrors: " + errors + "\n");
            Debug.Log("GOBLIN_UI_PLAYMODE_" + result + " errors=" + errors);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(result == "PASSED" ? 0 : 1);
        }
        private void OnLog(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        private IEnumerator Checks()
        {
            yield return null; yield return null;
            var demo = FindObjectOfType<UIShowcaseController>();
            var snapshot = demo.Mock.Current;
            bool layoutOnly = SessionState.GetBool("GoblinUI.LayoutOnly", false);
            SessionState.SetBool("GoblinUI.LayoutOnly", false);
            if (!layoutOnly)
            {
                Require(!demo.Theme.IsNight, "Initial theme should be Day.");
                yield return Click(demo.ThemeButton);
                Require(demo.Theme.IsNight && ReferenceEquals(snapshot, demo.Mock.Current), "Mouse theme switch must preserve data.");
                yield return KeyPress(Key.T);
                Require(!demo.Theme.IsNight, "T must change theme through Input System.");
                var selected = EventSystem.current.currentSelectedGameObject;
                yield return KeyPress(Key.Tab);
                Require(EventSystem.current.currentSelectedGameObject != selected, "Tab must move focus.");
                var disabled = demo.GetComponentsInChildren<UIButton>().Single(b => !b.Control.interactable && b.name == "UIButton_Primary");
                int clickCount = 0; disabled.Clicked.AddListener(() => clickCount++);
                yield return Click(disabled); Require(clickCount == 0, "Disabled button received click.");
                for (int i = 0; i < 6; i++)
                {
                    yield return Click(demo.VisibilityButton); Require(!demo.OrderPanel.gameObject.activeSelf, "Close failed.");
                    yield return Click(demo.VisibilityButton); Require(demo.OrderPanel.gameObject.activeSelf, "Reopen failed.");
                }
                var order = demo.OrderPanel.GetComponent<UIOrderCard>();
                Require(ReferenceEquals(order.Data, snapshot.Order), "Repeated open lost order data.");
                yield return Click(demo.DataButton); Require(demo.Mock.Current.Coins == 340, "Mock data click must fire once after reopen.");
                yield return Click(demo.DataButton); Require(order.Data.CanRequest && order.Data.Progress == 1, "Dynamic order snapshot did not update.");
                Require(demo.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "Coins").text == "360", "Currency presenter is stale.");
                yield return Click(order.GetComponentsInChildren<UIButton>().Single());
                Require(demo.Feedback.text.Contains("003") && demo.Mock.Current.Coins == 360, "Order action must only emit an intent.");
                var productionData = new GameData(); productionData.AddMoney(50);
                var adapter = new GameObject("Adapter Verification").AddComponent<UIGameDataSource>();
                adapter.Initialize(productionData); adapter.Publish(2, snapshot.Order, snapshot.Quests);
                Require(adapter.Current.Coins == 50 && productionData.Money == 50, "Formal adapter must read without mutating economy.");
                Destroy(adapter.gameObject);
                // Scene theme mapping is exercised via the same callback Unity invokes on sceneLoaded.
                var follow = typeof(UIThemeContext).GetField("followGameplayScenes", BindingFlags.NonPublic | BindingFlags.Instance);
                var sceneMethod = typeof(UIThemeContext).GetMethod("ApplyScene", BindingFlags.NonPublic | BindingFlags.Instance);
                follow.SetValue(demo.Theme, true);
                sceneMethod.Invoke(demo.Theme, new object[] { "Adventure" }); Require(demo.Theme.IsNight, "Adventure mapping failed.");
                sceneMethod.Invoke(demo.Theme, new object[] { "Bussiness" }); Require(!demo.Theme.IsNight, "Bussiness mapping failed.");
                follow.SetValue(demo.Theme, false);
                inputPassed = true;
            }
            else inputPassed = File.ReadAllText(output + "/verification.txt").Contains("Input / data / lifecycle checks: PASSED");
            if (SessionState.GetBool("GoblinUI.KeyboardCheck", false))
            {
                SessionState.SetBool("GoblinUI.KeyboardCheck", false);
                int submitCount = 0;
                demo.SecondaryButton.Clicked.AddListener(() => submitCount++);
                EventSystem.current.SetSelectedGameObject(demo.SecondaryButton.gameObject);
                yield return KeyPress(Key.Enter);
                Require(submitCount == 1, "Enter must submit exactly once.");
                yield return KeyPress(Key.Space);
                Require(submitCount == 2, "Space must submit exactly once.");
                Debug.Log("GOBLIN_UI_KEYBOARD_SUBMIT_OK: Enter + Space each submitted once.");
            }
            // Restore the intended authored sample before screenshots.
            if (!layoutOnly) demo.Mock.NextSample();
            demo.Feedback.text = "T 切换主题  /  R 刷新数据  /  O 打开或收起订单";
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) })
            {
                GoblinUIPlayModeVerification.SetResolution(size.x, size.y);
                yield return null; yield return null;
                Require(Screen.width == size.x && Screen.height == size.y, "Game View resolution mismatch.");
                foreach (bool night in new[] { false, true })
                {
                    demo.Theme.Apply(night);
                    yield return null; yield return null;
                    CheckLayout(demo);
                    var texture = CaptureGameView(size.x, size.y);
                    File.WriteAllBytes(output + "/showcase_" + (night ? "night_" : "day_") + size.x + "x" + size.y + ".png", texture.EncodeToPNG());
                    Destroy(texture); screenshots++;
                }
            }
            demo.Theme.ApplyDay();
        }
        private Texture2D CaptureGameView(int width, int height)
        {
            // In the Editor, WaitForEndOfFrame/ScreenCapture can observe the outer window
            // after Game View rendering. Read the actual fixed-resolution Game View target.
            var viewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(viewType);
            var target = (RenderTexture)viewType.GetField("m_RenderTexture", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(window);
            Require(target != null && target.width == width && target.height == height, "Game View render target size mismatch.");
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                if (SystemInfo.graphicsUVStartsAtTop)
                {
                    var source = texture.GetPixels32();
                    var upright = new Color32[source.Length];
                    for (int y = 0; y < height; y++) Array.Copy(source, y * width, upright, (height - y - 1) * width, width);
                    texture.SetPixels32(upright);
                }
                texture.Apply();
                return texture;
            }
            finally { RenderTexture.active = previous; }
        }
        private void CheckLayout(UIShowcaseController demo)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in demo.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate();
                Require(!text.isTextOverflowing, "Text overflow: " + text.name + ": " + text.text);
                Require(!text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c => c.character == '\u25a1'), "Missing Chinese glyph in " + text.name);
            }
            foreach (var name in new[] { "Workshop", "UIOrderCard", "UIQuestTracker", "DesignNotes" })
            {
                var rect = demo.GetComponentsInChildren<RectTransform>().Single(t => t.name == name);
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                Require(corners.All(c => c.x >= 0 && c.x <= Screen.width && c.y >= 0 && c.y <= Screen.height), "Panel outside screen: " + name + " screen=" + Screen.width + "x" + Screen.height + " safe=" + Screen.safeArea + " corners=" + string.Join(";", corners.Select(c => c.ToString())));
            }
        }
        private IEnumerator KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
        }
        private IEnumerator Click(UIButton button)
        {
            var rect = (RectTransform)button.transform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
