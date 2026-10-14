using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Untitled.UI;

namespace Untitled.Editor
{
    [InitializeOnLoad]
    public static class GoblinUIHUDVerification
    {
        private const string Pending = "GoblinUI.HUDVerification";
        static GoblinUIHUDVerification() => EditorApplication.playModeStateChanged += OnState;
        public static void Run()
        {
            EditorSceneManager.OpenScene(GoblinUIBuilder.HUDScenePath);
            GoblinUIPlayModeVerification.SetResolution(1920, 1080);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void OnState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            var go = new GameObject("HUD Verification (temporary)"); UnityEngine.Object.DontDestroyOnLoad(go); go.AddComponent<GoblinUIHUDVerificationRunner>();
        }
    }
    public sealed class GoblinUIHUDVerificationRunner : MonoBehaviour
    {
        private Mouse mouse;
        private Keyboard keyboard;
        private int errors, captures;
        private bool failed;
        private string output;
        private IEnumerator Start()
        {
            output = Path.GetFullPath("docs/images/ui_hud"); Directory.CreateDirectory(output);
            mouse = InputSystem.AddDevice<Mouse>("HUD Verification Mouse"); keyboard = InputSystem.AddDevice<Keyboard>("HUD Verification Keyboard");
            Application.logMessageReceived += OnLog;
            var checks = Checks();
            while (true)
            {
                object current;
                try { if (!checks.MoveNext()) break; current = checks.Current; }
                catch (Exception exception) { Debug.LogException(exception); failed = true; break; }
                yield return current;
            }
            InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard); Application.logMessageReceived -= OnLog;
            var result = failed || errors > 0 ? "FAILED" : "PASSED";
            File.WriteAllText(output + "/verification.txt", "Unity Editor Play Mode: " + result + "\nErrors: " + errors + "\nScreenshots: " + captures + "\n");
            Debug.Log("GOBLIN_UI_HUD_PLAYMODE_" + result + " errors=" + errors);
            EditorApplication.isPlaying = false; EditorApplication.delayCall += () => EditorApplication.Exit(result == "PASSED" ? 0 : 1);
        }
        private void OnLog(string text, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        private IEnumerator Checks()
        {
            yield return null; yield return null;
            var mock = FindObjectOfType<UIHUDMockSource>(); var hud = FindObjectOfType<UIGameplayHUD>();
            Require(!hud.Health.gameObject.activeSelf && hud.OrderRoot.activeSelf, "Day HUD visibility incorrect.");
            mock.TogglePhase(); yield return null; yield return null;
            Require(hud.Health.Fraction == 1 && Mathf.Approximately(hud.Resource.Fraction, .8f) && !hud.Crosshair.activeSelf, "Night status projection failed.");
            Require(hud.PhaseIcon.sprite == hud.Theme.Current.PhaseIcon, "Night phase icon was stale.");
            hud.Health.Bind(new UIStatusData("血量", 20, 0)); Require(hud.Health.Fraction == 0, "Zero max status must not divide by zero.");
            mock.ToggleAlert(); yield return null; Require(hud.Alert.activeSelf, "Alert was not shown.");
            int requests = 0; hud.Quickbar.ActionRequested.AddListener(id => requests++);
            yield return KeyPress(Key.Digit1); Require(requests == 1, "Numeric quickbar request failed.");
            yield return KeyPress(Key.Escape);
            Require(GameManager.Instance.IsPaused && UIManager.Instance.Top is MenuPanel, "Esc must open existing menu stack.");
            yield return KeyPress(Key.Digit1); Require(requests == 1, "Quickbar accepted input behind modal.");
            var pause = UIManager.Instance.Top.GetComponent<UIPauseMenu>();
            yield return Click(pause.HelpButton);
            if (!pause.Help.gameObject.activeSelf) Capture("failed_pause", new Vector2Int(1920, 1080));
            Require(pause.Help.gameObject.activeSelf, "Pause help button failed.");
            yield return KeyPress(Key.Tab); Require(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(pause.transform), "Pause focus escaped.");
            yield return KeyPress(Key.Escape); Require(!GameManager.Instance.IsPaused && UIManager.Instance.Top == null, "Esc resume failed.");
            for (int i = 0; i < 3; i++) { hud.OpenPause(); yield return null; yield return Click(UIManager.Instance.Top.GetComponent<UIPauseMenu>().ResumeButton); }
            Require(!GameManager.Instance.IsPaused, "Repeated resume stuck pause state.");
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) })
            {
                GoblinUIPlayModeVerification.SetResolution(size.x, size.y); yield return null; yield return null;
                foreach (bool night in new[] { false, true })
                {
                    if (mock.Current.Night != night) mock.TogglePhase();
                    yield return null; yield return null; CheckText(hud.gameObject);
                    Capture("demo_" + (night ? "night_" : "day_") + size.x + "x" + size.y, size);
                }
            }
            GoblinUIPlayModeVerification.SetResolution(1920, 1080); yield return null; yield return null;
            // Load actual authored gameplay scenes with their original players and cameras.
            var shopLoad = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Gameplay/Bussiness.unity", new LoadSceneParameters(LoadSceneMode.Single));
            while (!shopLoad.isDone) yield return null;
            yield return RenderReady();
            hud = FindObjectOfType<UIGameplayHUD>(); var source = FindObjectOfType<UIGameplayReadModel>(); var player = FindObjectOfType<ShopFirstPersonController>();
            Require(hud != null && player != null && !source.Current.Night, "Actual shop HUD attachment failed.");
            var data = GameManager.Instance.GameData; data.AddMoney(37);
            int itemId = DatabaseManager.Instance.ItemDatabase.All.Keys.First(); data.InventoryData.AddItem(itemId, 4);
            source.Publish(new UIHUDData(4, data.Money, false, new UIOrderData("preserved", "抹茶奶茶", "客人", "茶叶", "等待", .4f, false), Array.Empty<UIQuestData>()));
            yield return null;
            Require(source.Current.Coins == 37, "Real money event did not update HUD.");
            var position = player.transform.position; var eye = player.Eye.localRotation;
            yield return KeyPress(Key.Escape); Require(player.UIBlocked && Cursor.lockState == CursorLockMode.None && GameManager.Instance.IsPaused, "Shop pause did not block look/unlock cursor.");
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(160, 40) }); yield return null; yield return null;
            Require(player.Eye.localRotation == eye && Vector3.Distance(player.transform.position, position) < .02f, "Shop moved/looked while paused.");
            CheckText(hud.gameObject); Capture("shop_pause_1920x1080", new Vector2Int(1920, 1080));
            yield return KeyPress(Key.Escape); Require(!player.UIBlocked && !GameManager.Instance.IsPaused, "Shop resume did not restore input.");
            var shop = FindObjectOfType<ShopSceneBindings>(); shop.Stations.First(s => s.Kind == ShopStationKind.Storage).Interact(); yield return null;
            Require(UIManager.Instance.Top is UIInventoryWindow && UIManager.Instance.Top.GetComponent<UIInventoryWindow>().View.Data.Items.First().Quantity == 4, "Storage station did not show real warehouse.");
            UIManager.Instance.CloseAll();
            shop.Stations.First(s => s.Kind == ShopStationKind.Preparation).Interact(); yield return null;
            Require(UIManager.Instance.Top is UIShopWorkPanel && UIManager.Instance.Top.GetComponent<UIShopWorkPanel>().View.Data.Id == "preserved", "Preparation did not receive order snapshot.");
            UIManager.Instance.CloseAll(); yield return null; yield return null;
            Capture("shop_hud_1920x1080", new Vector2Int(1920, 1080));
            var caveLoad = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Gameplay/Adventure.unity", new LoadSceneParameters(LoadSceneMode.Single));
            while (!caveLoad.isDone) yield return null;
            yield return RenderReady();
            source = FindObjectOfType<UIGameplayReadModel>(); hud = FindObjectOfType<UIGameplayHUD>();
            Require(ReferenceEquals(data, GameManager.Instance.GameData) && data.Money == 37 && data.InventoryData.GetItemCount(itemId) == 4, "Scene change reset game data.");
            Require(source.Current.Night && source.Current.Order.Id == "preserved" && source.Current.Day == 4, "Scene theme binding reset display snapshot.");
            Require(!hud.Health.gameObject.activeSelf, "Production scene fabricated health without a status provider.");
            CheckText(hud.gameObject); Capture("adventure_hud_1920x1080", new Vector2Int(1920, 1080));
            yield return KeyPress(Key.Escape); Require(GameManager.Instance.IsPaused, "Adventure pause failed.");
            var adventurePlayer = FindObjectOfType<PlayerEntity>(); var cavePosition = adventurePlayer.transform.position;
            yield return KeyPress(Key.W); Require(Vector3.Distance(cavePosition, adventurePlayer.transform.position) < .02f, "Adventure player moved while paused.");
            yield return KeyPress(Key.Escape); Require(!GameManager.Instance.IsPaused, "Adventure resume failed.");
            Debug.Log("GOBLIN_UI_HUD_INPUT_SCENE_STATE_OK");
        }
        private void CheckText(GameObject root)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(false)) { text.ForceMeshUpdate(); Require(!text.isTextOverflowing, "Text overflow: " + text.name + ": " + text.text); }
        }
        private IEnumerator RenderReady()
        {
            yield return new WaitForSecondsRealtime(2);
            while (ShaderUtil.anythingCompiling) yield return null;
            yield return null; yield return null;
            Debug.Log("HUD_CAMERA " + Camera.main.name + " active=" + Camera.main.isActiveAndEnabled + " position=" + Camera.main.transform.position + " renderers=" + FindObjectsOfType<Renderer>().Length);
        }
        private void Capture(string name, Vector2Int size)
        { var image = GoblinUIVerificationRunner.CaptureGameView(size.x, size.y); File.WriteAllBytes(output + "/" + name + ".png", image.EncodeToPNG()); Destroy(image); captures++; }
        private IEnumerator KeyPress(Key key)
        { InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null; yield return null; InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null; }
        private IEnumerator Click(UIButton button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform; Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Debug.Log("HUD_CLICK " + button.name + " position=" + point + " lock=" + Cursor.lockState + " hits=" + string.Join(",", hits.Select(hit => hit.gameObject.name)));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
