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
using UnityEngine.UI;
using Untitled.UI;

namespace Untitled.Editor
{
    [InitializeOnLoad]
    public static class GoblinUIExtendedVerification
    {
        private const string Pending = "GoblinUI.ExtendedVerification";
        static GoblinUIExtendedVerification() => EditorApplication.playModeStateChanged += OnPlayMode;
        public static void RunLayoutOnly()
        {
            SessionState.SetBool("GoblinUI.ExtendedLayoutOnly", true);
            Run();
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(GoblinUIBuilder.ExtendedScenePath);
            GoblinUIValidation.Validate();
            foreach (var name in new[] { "UIItemSlot", "UIInventoryPanel", "UIItemTooltip", "UIDialogueBox", "UIDialogueChoice", "UIRewardPopup", "UI_Dialogue" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoblinUIBuilder.Prefabs + "/" + name + ".prefab");
                if (prefab == null || prefab.GetComponentsInChildren<Transform>(true).Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)) throw new InvalidOperationException("Invalid extended prefab: " + name);
            }
            GoblinUIPlayModeVerification.SetResolution(1920, 1080);
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Extended Verification (temporary)").AddComponent<GoblinUIExtendedVerificationRunner>();
        }
    }
    public sealed class GoblinUIExtendedVerificationRunner : MonoBehaviour
    {
        private Mouse mouse;
        private Keyboard keyboard;
        private int errors, captures;
        private bool failed;
        private string output;
        private IEnumerator Start()
        {
            output = Path.GetFullPath("docs/images/ui_extended"); Directory.CreateDirectory(output);
            Application.logMessageReceived += OnLog;
            mouse = InputSystem.AddDevice<Mouse>("Extended UI Verification Mouse"); keyboard = InputSystem.AddDevice<Keyboard>("Extended UI Verification Keyboard");
            var checks = Checks();
            while (true)
            {
                object current;
                try { if (!checks.MoveNext()) break; current = checks.Current; }
                catch (Exception exception) { Debug.LogException(exception); failed = true; break; }
                yield return current;
            }
            InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard);
            Application.logMessageReceived -= OnLog;
            string result = failed || errors > 0 ? "FAILED" : "PASSED";
            File.WriteAllText(output + "/verification.txt", "Unity Editor Play Mode: " + result + "\nErrors: " + errors + "\nScreenshots: " + captures + "\n");
            Debug.Log("GOBLIN_UI_EXTENDED_PLAYMODE_" + result + " errors=" + errors);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(result == "PASSED" ? 0 : 1);
        }
        private void OnLog(string text, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        private IEnumerator Checks()
        {
            yield return null; yield return null;
            var demo = FindObjectOfType<UIExtendedShowcaseController>();
            bool layoutOnly = SessionState.GetBool("GoblinUI.ExtendedLayoutOnly", false);
            SessionState.SetBool("GoblinUI.ExtendedLayoutOnly", false);
            if (!layoutOnly)
            {
                Require(demo.Inventory.Data.Items.Count == 8 && demo.Inventory.Slots.Count == 12, "Inventory snapshot/empty slots mismatch.");
                int actions = 0; demo.Inventory.ItemRequested.AddListener(id => actions++);
                var first = demo.Inventory.Slots[0]; int quantity = first.Data.Quantity;
                yield return Click(first.Button);
                Require(demo.Tooltip.Data.Id == "milk" && actions == 1 && first.Data.Quantity == quantity, "Item click must preview and emit one intent without consuming.");
                EventSystem.current.SetSelectedGameObject(first.gameObject);
                yield return KeyPress(Key.Space); Require(actions == 2, "Item keyboard action did not submit once.");
                demo.Inventory.ShowQuests(); yield return null;
                Require(demo.Inventory.Slots[0].Data.Category == "任务" && demo.Inventory.Slots[1].Data == null, "Category filter mismatch.");
                yield return Click(demo.Inventory.Slots[0].Button); Require(actions == 2 && demo.Tooltip.Data.Id == "pass", "Read-only quest item must remain inspectable without action.");
                demo.Inventory.ShowAll();
                for (int i = 0; i < 4; i++) { yield return Click(demo.InventoryButton); yield return Click(demo.InventoryButton); }
                yield return Click(demo.Inventory.Slots[0].Button); Require(actions == 3, "Repeated open duplicated item action subscriptions.");
                demo.Mock.NextSample(); demo.Mock.NextSample(); yield return null; yield return null;
                Require(demo.Inventory.Data.Items.Count == 20 && demo.Inventory.Slots.Count == 20, "Large inventory lost entries.");
                demo.Inventory.Bind(new UIInventorySnapshot("空背包", Array.Empty<UIItemData>())); yield return null;
                Require(demo.Inventory.Slots.All(s => !s.gameObject.activeSelf || s.Data == null), "Empty inventory retained stale items.");
                demo.Inventory.Bind(demo.Mock.Current);
                var snapshot = demo.Inventory.Data; demo.Theme.Toggle();
                Require(ReferenceEquals(snapshot, demo.Inventory.Data), "Theme switch changed inventory data.");
                demo.Theme.Toggle();
                var tooltipRect = (RectTransform)demo.Tooltip.transform;
                demo.Tooltip.ShowAt(snapshot.Items[0], new Vector2(Screen.width - 1, 1));
                var corners = new Vector3[4]; tooltipRect.GetWorldCorners(corners);
                Require(corners.All(c => c.x >= 0 && c.x <= Screen.width && c.y >= 0 && c.y <= Screen.height), "Floating tooltip escaped screen.");
                tooltipRect.anchoredPosition = new Vector2(860, -220); demo.Tooltip.Show(snapshot.Items[4]);
                int choices = 0; demo.Dialogue.ChoiceRequested.AddListener(id => choices++);
                demo.OpenDialogue(); yield return null;
                yield return Click(demo.Dialogue.ChoiceButtons[1]); Require(choices == 0, "Disabled dialogue choice fired.");
                yield return Click(demo.Dialogue.ChoiceButtons[0]); Require(choices == 1 && demo.Dialogue.Data.Choices.Count == 0, "Dialogue choice did not transition through host.");
                yield return Click(demo.Dialogue.ContinueButton); Require(!demo.Dialogue.gameObject.activeSelf, "Continue did not close demo dialogue.");
                for (int i = 0; i < 4; i++) { demo.OpenDialogue(); demo.Dialogue.SetVisible(false); }
                demo.OpenDialogue(); yield return null;
                yield return Click(demo.Dialogue.ChoiceButtons[0]); Require(choices == 2, "Dialogue reopen duplicated choice listeners.");
                demo.OpenDialogue();
                int rewardReads = 0; demo.Reward.Acknowledged.AddListener(id => rewardReads++);
                yield return Click(demo.RewardButton);
                Require(demo.Reward.gameObject.activeSelf && EventSystem.current.currentSelectedGameObject == demo.Reward.ConfirmButton.gameObject, "Reward modal did not take focus.");
                var rewardData = demo.Reward.Data; demo.Theme.Toggle();
                Require(ReferenceEquals(rewardData, demo.Reward.Data), "Reward theme switch lost data.");
                demo.Theme.Toggle();
                int previousActions = actions;
                yield return Click(demo.Inventory.Slots[0].Button); Require(actions == previousActions, "Modal backdrop allowed click-through.");
                yield return KeyPress(Key.Tab);
                Require(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(demo.Reward.transform), "Modal focus escaped.");
                yield return KeyPress(Key.Escape); Require(!demo.Reward.gameObject.activeSelf, "Esc did not close modal.");
                Require(EventSystem.current.currentSelectedGameObject == demo.RewardButton.gameObject, "Modal did not restore focus.");
                for (int i = 0; i < 3; i++) { demo.OpenReward(); yield return Click(demo.Reward.ConfirmButton); }
                Require(rewardReads == 3 && demo.Inventory.Data.Items.Count == 20, "Reward acknowledge duplicated or modified inventory.");
                // Exercise the actual existing tables and UIManager address, not mock dialogue rules.
                var database = new DatabaseManager(); database.Initialize();
                var inventoryData = new InventoryData(); var catalogItem = database.ItemDatabase.All.Values.First(); inventoryData.AddItem(catalogItem.Id, 7);
                var adapter = new GameObject("Inventory Adapter Check").AddComponent<UIInventoryDataSource>();
                adapter.Initialize(inventoryData.Items, database.ItemDatabase, address => null, "仓库"); adapter.Publish();
                Require(adapter.Current.Items[0].Name == catalogItem.Name && adapter.Current.Items[0].Quantity == 7 && inventoryData.GetItemCount(catalogItem.Id) == 7, "Formal inventory adapter changed data or missed table name.");
                var game = new GameManager(); game.Initialize();
                var manager = new UIManager(); manager.Initialize(); manager.SetRoot(demo.transform);
                var node = database.DialogDatabase.All.Values.First();
                var formal = manager.Open<DialoguePanel>(Untitled.UIPanel.Dialogue, new DialogRequest(node.Id));
                yield return null;
                Require(formal != null && formal.IsOpen && formal.GetComponent<UIDialogueBox>().Data.Line == node.Text && game.IsPaused, "Existing dialogue/UIManager path failed.");
                manager.Close(formal); manager.UnloadAll(); Require(!game.IsPaused, "Formal dialogue pause did not restore.");
                Destroy(adapter.gameObject);
            }
            // Additional checks for keyboard visibility and long-content scrolling.
            var manyItems = Enumerable.Range(0, 20).Select(i => new UIItemData("key" + i, "茶叶", "材料", "键盘滚动检查", 1)).ToArray();
            demo.Inventory.Bind(new UIInventorySnapshot("键盘滚动", manyItems));
            EventSystem.current.SetSelectedGameObject(demo.Inventory.Slots[19].gameObject);
            yield return null; yield return null;
            var lastRect = (RectTransform)demo.Inventory.Slots[19].transform;
            var itemView = demo.Inventory.GetComponentInChildren<ScrollRect>().viewport;
            var itemCorners = new Vector3[4]; lastRect.GetWorldCorners(itemCorners);
            Require(itemView.InverseTransformPoint(itemCorners[0]).y >= itemView.rect.yMin - 1 && itemView.InverseTransformPoint(itemCorners[2]).y <= itemView.rect.yMax + 1, "Keyboard-selected item was clipped.");
            demo.Inventory.Bind(demo.Mock.Current);
            demo.Dialogue.Bind(new UIDialogueData("长对话", string.Concat(Enumerable.Repeat("王冠也是借来的。", 40))));
            demo.Dialogue.FocusDefault(); yield return null; yield return null;
            var bodyScroll = demo.Dialogue.GetComponentsInChildren<ScrollRect>(true).First(s => s.name == "LineViewport"); bodyScroll.verticalNormalizedPosition = 1;
            yield return KeyPress(Key.PageDown); Require(bodyScroll.verticalNormalizedPosition < 1, "Long dialogue keyboard scrolling failed.");
            demo.OpenReward();
            demo.Reward.Open(new UIRewardData("scroll-test", "奖励列表", "奖励显示滚动检查", manyItems.Take(9).ToArray()));
            yield return null; yield return null;
            var rewardScroll = demo.Reward.GetComponentInChildren<ScrollRect>(); rewardScroll.verticalNormalizedPosition = 1;
            yield return KeyPress(Key.PageDown); Require(rewardScroll.verticalNormalizedPosition < 1, "Reward keyboard scrolling failed.");
            demo.Reward.Close();
            demo.Reward.Open(null);
            yield return KeyPress(Key.Escape); Require(!demo.Reward.gameObject.activeSelf, "Empty reward could not close.");
            Debug.Log("GOBLIN_UI_EXTENDED_KEYBOARD_SCROLL_OK");
            if (!layoutOnly) demo.Mock.NextSample();
            demo.Inventory.ShowAll(); demo.OpenDialogue(); demo.Tooltip.Show(demo.Mock.Current.Items[4]);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = Vector2.zero });
            demo.Feedback.text = "选择物品查看详情 · Enter / 空格确认 · 奖励弹窗中 Esc 关闭";
            EventSystem.current.SetSelectedGameObject(demo.ThemeButton.gameObject);
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) })
            {
                GoblinUIPlayModeVerification.SetResolution(size.x, size.y); yield return null; yield return null;
                foreach (bool night in new[] { false, true })
                {
                    demo.Theme.Apply(night); yield return null; yield return null;
                    CheckText(demo); SaveCapture((night ? "night_" : "day_") + size.x + "x" + size.y, size);
                    demo.OpenReward(); yield return null; yield return null;
                    CheckText(demo); SaveCapture("reward_" + (night ? "night_" : "day_") + size.x + "x" + size.y, size);
                    demo.Reward.Close();
                }
            }
            Debug.Log(layoutOnly ? "GOBLIN_UI_EXTENDED_LAYOUT_ONLY_OK" : "GOBLIN_UI_EXTENDED_INPUT_DATA_FORMAL_INTEGRATION_OK");
        }
        private void CheckText(UIExtendedShowcaseController demo)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var close in demo.GetComponentsInChildren<UIButton>(false).Where(b => b.name == "Close"))
            {
                var label = close.GetComponentInChildren<TMP_Text>(); label.ForceMeshUpdate();
                Require(label.textInfo.characterInfo.Take(label.textInfo.characterCount).Any(c => c.character == '×' && c.isVisible), "Close glyph is invisible.");
            }
            foreach (var text in demo.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate(); Require(!text.isTextOverflowing, "Text overflow: " + text.name + " / " + text.text);
                Require(!text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c => c.character == '\u25a1'), "Missing glyph in " + text.name);
            }
            foreach (var rect in new[] { (RectTransform)demo.Inventory.transform, (RectTransform)demo.Tooltip.transform, (RectTransform)demo.Dialogue.transform })
            {
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                Require(corners.All(c => c.x >= 0 && c.x <= Screen.width && c.y >= 0 && c.y <= Screen.height), "Panel outside screen: " + rect.name);
            }
        }
        private void SaveCapture(string name, Vector2Int size)
        {
            var texture = GoblinUIVerificationRunner.CaptureGameView(size.x, size.y);
            File.WriteAllBytes(output + "/" + name + ".png", texture.EncodeToPNG()); Destroy(texture); captures++;
        }
        private IEnumerator KeyPress(Key key)
        { InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null; yield return null; InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null; }
        private IEnumerator Click(UIButton button)
        {
            var rect = (RectTransform)button.transform; Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
