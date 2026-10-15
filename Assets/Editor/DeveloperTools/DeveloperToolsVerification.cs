using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Untitled.UI;

namespace Untitled.Editor
{
    // Opt-in checks use the existing project verification pattern and an isolated save file.
    [InitializeOnLoad]
    public static class DeveloperToolsVerification
    {
        private const string Pending = "Untitled.DeveloperTools.Verification.Pending";
        private const string OriginalScenes = "Untitled.DeveloperTools.Verification.Scenes";
        private const string RestorePending = "Untitled.DeveloperTools.Verification.Restore";
        public static string TestDirectory => Path.GetFullPath("Library/DeveloperToolsVerification");
        public static string SavePath => Path.Combine(TestDirectory, "ordinary-save-sentinel.json");

        [Serializable] private sealed class SavedSetup { public SavedScene[] Scenes; }
        [Serializable] private sealed class SavedScene { public string Path; public bool Loaded; public bool Active; }

        static DeveloperToolsVerification() => EditorApplication.playModeStateChanged += OnPlayMode;

        [MenuItem("Untitled/Developer Tools/Development Tests/Run Phase 1 Checks")]
        public static void RunPlayChecks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play Mode。 ");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty || (string.IsNullOrEmpty(scene.path) && scene.rootCount > 0))
                    throw new InvalidOperationException("请先保存当前场景，再运行会切换测试场景的检查。");
            }
            SessionState.SetString(OriginalScenes, JsonUtility.ToJson(new SavedSetup
            {
                Scenes = setup.Select(s => new SavedScene { Path = s.path, Loaded = s.isLoaded, Active = s.isActive }).ToArray()
            }));
            SessionState.SetBool(RestorePending, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                var runner = new GameObject("DEVELOPMENT ONLY - Developer Toolkit Verification");
                UnityEngine.Object.DontDestroyOnLoad(runner);
                runner.AddComponent<DeveloperToolsVerificationRunner>();
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RestorePending, false))
            {
                SessionState.SetBool(RestorePending, false);
                EditorApplication.delayCall += RestoreSetup;
            }
        }

        private static void RestoreSetup()
        {
            var saved = JsonUtility.FromJson<SavedSetup>(SessionState.GetString(OriginalScenes, "{}"));
            if (saved?.Scenes == null || saved.Scenes.Length == 0) return;
            if (saved.Scenes.Any(s => string.IsNullOrEmpty(s.Path))) return; // Original empty, unsaved scene.
            EditorSceneManager.RestoreSceneManagerSetup(saved.Scenes.Select(s => new SceneSetup
                { path = s.Path, isLoaded = s.Loaded, isActive = s.Active }).ToArray());
        }

        [MenuItem("Untitled/Developer Tools/Development Tests/Run Edit Mode Guards")]
        public static void RunEditModeChecks()
        {
            Require(!EditorApplication.isPlaying, "Run outside Play Mode.");
            var service = DebugCommandService.Current;
            Require(!service.TryInspect(out _, out _), "Edit mode unexpectedly has a live session.");
            Require(!service.Execute(new DebugCommandRequest(DebugCommandKind.AddMoney, 1)).Success, "Edit mode mutation accepted.");
            Require(!service.Execute(null).Success, "Null command accepted.");
            Require(!service.Execute(new DebugCommandRequest((DebugCommandKind)999)).Success, "Unknown command accepted.");
            Require(service.History.Count >= 3, "Failed commands were not logged.");
            Debug.Log("DEVELOPER_TOOLS_EDIT_GUARDS_PASSED");
        }

        public static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[DeveloperToolsVerification] " + message);
        }
    }

    public sealed class DeveloperToolsVerificationRunner : MonoBehaviour
    {
        private int errors;
        private IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            bool passed = true;
            var checks = Checks();
            while (true)
            {
                object next;
                try { if (!checks.MoveNext()) break; next = checks.Current; }
                catch (Exception exception) { passed = false; Debug.LogException(exception); break; }
                yield return next;
            }
            Application.logMessageReceived -= OnLog;
            passed &= errors == 0;
            Directory.CreateDirectory(DeveloperToolsVerification.TestDirectory);
            File.WriteAllText(Path.Combine(DeveloperToolsVerification.TestDirectory, "phase1.txt"),
                (passed ? "PASSED" : "FAILED") + "\nErrors: " + errors + "\n");
            Debug.Log("DEVELOPER_TOOLS_PHASE1_" + (passed ? "PASSED" : "FAILED"));
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 1);
        }
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }

        private static DebugCommandResult Run(DebugCommandKind kind, int value = 0, int item = 0,
            GamePhase phase = GamePhase.Shop, bool confirmed = false)
            => DebugCommandService.Current.Execute(new DebugCommandRequest(kind, value, item, phase, confirmed));
        private static void Require(bool condition, string message) => DeveloperToolsVerification.Require(condition, message);

        private IEnumerator Checks()
        {
            var service = DebugCommandService.Current;
            Require(!Run(DebugCommandKind.AddMoney, 1).Success, "Missing session silently succeeded.");
            Require(!service.TryInspect(out _, out _), "Missing session inspection succeeded.");
            var database = new DatabaseManager(); database.Initialize();
            int id = database.ItemDatabase.All.Keys.First();
            int missingId = int.MaxValue;
            while (database.ItemDatabase.All.ContainsKey(missingId)) missingId--;
            Directory.CreateDirectory(DeveloperToolsVerification.TestDirectory);
            Require(new GameSaveStore(DeveloperToolsVerification.SavePath).TrySave(new GameData().Capture(), out _), "Sentinel creation failed.");
            byte[] before = File.ReadAllBytes(DeveloperToolsVerification.SavePath);
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            var settings = ScriptableObject.CreateInstance<InventorySettings>();
            var itemConfig = new SerializedObject(definition);
            itemConfig.FindProperty("itemId").intValue = id;
            itemConfig.FindProperty("maxStack").intValue = 6;
            itemConfig.ApplyModifiedPropertiesWithoutUndo();
            var inventoryConfig = new SerializedObject(settings);
            inventoryConfig.FindProperty("limitCapacity").boolValue = true;
            inventoryConfig.FindProperty("capacity").intValue = 1;
            inventoryConfig.FindProperty("items").arraySize = 1;
            inventoryConfig.FindProperty("items").GetArrayElementAtIndex(0).objectReferenceValue = definition;
            inventoryConfig.ApplyModifiedPropertiesWithoutUndo();
            var manager = new GameManager(); manager.Configure(settings, DeveloperToolsVerification.SavePath); manager.Initialize();
            Destroy(settings); Destroy(definition);
            Require(!manager.DeveloperStateModified, "Fresh session save guard unexpectedly locked.");
            var game = manager.GameData;
            Require(Run(DebugCommandKind.GetState).Success && Run(DebugCommandKind.GetInventory).Success, "Read commands failed.");
            Require(!manager.DeveloperStateModified, "Read command locked normal saves.");
            Require(!Run(DebugCommandKind.AddMoney, 0).Success && !Run(DebugCommandKind.SetMoney, -1).Success, "Invalid money accepted.");
            Require(!Run(DebugCommandKind.GiveItem, 1, missingId).Success && !Run(DebugCommandKind.GiveItem, -1, id).Success, "Invalid item accepted.");
            Require(!Run(DebugCommandKind.GiveItem, 7, id).Success && game.InventoryData.Items.Count == 0, "Capacity failure partially changed inventory.");
            Require(!Run(DebugCommandKind.RemoveItem, 1, id).Success, "Absent inventory removed.");
            Require(!Run(DebugCommandKind.SetDay, 0).Success && !Run(DebugCommandKind.SetDayMetadata, 8, confirmed: true).Success, "Invalid Day accepted.");
            Require(Run(DebugCommandKind.SetDay, 7).Status == DebugCommandStatus.Unavailable && game.Day == 1, "Day readiness was fabricated.");
            Require(Run(DebugCommandKind.SetDayMetadata, 7).Status == DebugCommandStatus.ConfirmationRequired && game.Day == 1, "Day metadata bypassed confirmation.");
            Require(Run(DebugCommandKind.ClearInventory).Status == DebugCommandStatus.ConfirmationRequired, "Clear bypassed confirmation.");
            Require(!Run(DebugCommandKind.SetPhase, phase: (GamePhase)999).Success && game.Phase == GamePhase.Shop, "Invalid phase accepted.");
            Require(!Run(DebugCommandKind.SetPhase, phase: GamePhase.Shop).Success, "Empty scene counted as a ready shop.");
            Require(!manager.DeveloperStateModified, "Rejected commands locked ordinary saves.");

            var shopLoad = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Bussiness.unity", new LoadSceneParameters(LoadSceneMode.Single));
            while (!shopLoad.isDone) yield return null;
            yield return null; yield return null;
            Require(ReferenceEquals(manager, GameManager.Instance), "Shop Bootstrap duplicated GameManager.");
            var hud = FindObjectOfType<UIGameplayReadModel>();
            var shop = FindObjectOfType<ShopSceneBindings>();
            Require(hud != null && shop != null, "Real shop/HUD missing.");
            Require(Run(DebugCommandKind.SetPhase, phase: GamePhase.Shop).Success, "Ready shop phase rejected.");
            Require(!Run(DebugCommandKind.SetPhase, phase: GamePhase.Underground).Success && game.Phase == GamePhase.Shop, "Phase changed into an absent scene.");
            bool saveBlockedInsideEvent = false;
            Action<int> saveObserver = coins => saveBlockedInsideEvent = !manager.TrySave(out _)
                && SaveManager.Instance.Save().Error == SaveError.NotReady;
            DebugCommandResult nested = null;
            Action<int> reentrantObserver = coins => nested = Run(DebugCommandKind.AddMoney, 1);
            game.MoneyChanged += saveObserver;
            game.MoneyChanged += reentrantObserver;
            Require(Run(DebugCommandKind.AddMoney, 37).Success && saveBlockedInsideEvent, "Save guard was raised after UI/game events.");
            game.MoneyChanged -= saveObserver;
            game.MoneyChanged -= reentrantObserver;
            Require(nested != null && !nested.Success && game.Money == 37 && service.History.Contains(nested), "Reentrant command mutated state or was not logged.");
            Require(hud.Current.Coins == 37 && manager.DeveloperStateModified, "Money command did not refresh original HUD.");
            Require(Run(DebugCommandKind.SetMoney, 12).Success && hud.Current.Coins == 12, "Set-money did not refresh HUD.");
            Require(Run(DebugCommandKind.SetMoney, 0).Success && hud.Current.Coins == 0, "Zero money setting failed.");
            Require(Run(DebugCommandKind.SetMoney, int.MaxValue).Success && !Run(DebugCommandKind.AddMoney, 1).Success && game.Money == int.MaxValue, "Money overflow changed state.");
            Require(Run(DebugCommandKind.SetMoney, 37).Success, "Restore test balance failed.");
            Require(Run(DebugCommandKind.GiveItem, 4, id).Success, "Valid item grant failed.");
            shop.Stations.First(s => s.Kind == ShopStationKind.Storage).Interact();
            yield return null;
            var window = UIManager.Instance.Top as UIInventoryWindow;
            Require(window != null && window.View.Data.Items.Single().Quantity == 4, "Shop storage read model missing.");
            Require(Run(DebugCommandKind.GiveItem, 2, id).Success && window.View.Data.Items.Single().Quantity == 6
                && window.View.Slots.First().Data.Quantity == 6, "Grant did not refresh the open original inventory/slot.");
            Require(Run(DebugCommandKind.RemoveItem, 3, id).Success && window.View.Data.Items.Single().Quantity == 3, "Remove did not refresh inventory.");
            Require(!Run(DebugCommandKind.RemoveItem, 4, id).Success && window.View.Data.Items.Single().Quantity == 3, "Insufficient removal partially mutated.");
            var detached = Run(DebugCommandKind.GetInventory).State;
            Require(Run(DebugCommandKind.SetDayMetadata, 7, confirmed: true).Success && hud.Current.Day == 7, "Confirmed metadata did not refresh HUD.");
            Require(Run(DebugCommandKind.ClearInventory, confirmed: true).Success && window.View.Data.Items.Count == 0
                && detached.Items.Single().Count == 3, "Clear failed or snapshot references live mutable state.");
            UIManager.Instance.CloseAll();
            Require(!manager.TrySave(out string reason) && !string.IsNullOrEmpty(reason), "Debug data reached ordinary save entry.");
            Require(File.ReadAllBytes(DeveloperToolsVerification.SavePath).SequenceEqual(before), "Ordinary save sentinel overwritten.");

            var caveLoad = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Adventure.unity", new LoadSceneParameters(LoadSceneMode.Single));
            while (!caveLoad.isDone) yield return null;
            yield return null; yield return null;
            Require(ReferenceEquals(manager, GameManager.Instance) && ReferenceEquals(game, manager.GameData), "Scene change recreated manager/data.");
            Require(Run(DebugCommandKind.SetPhase, phase: GamePhase.Underground).Success
                && game.Phase == GamePhase.Underground && FindObjectOfType<UIGameplayReadModel>().Current.Night, "Underground phase/HUD not consistent.");
            Require(!Run(DebugCommandKind.SetPhase, phase: GamePhase.Settlement).Success && game.Phase == GamePhase.Underground, "Missing settlement fabricated.");
            Require(manager.DeveloperStateModified && !manager.TrySave(out _), "Scene change lost save guard.");
            var returnLoad = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Bussiness.unity", new LoadSceneParameters(LoadSceneMode.Single));
            while (!returnLoad.isDone) yield return null;
            yield return null; yield return null;
            Require(ReferenceEquals(manager, GameManager.Instance) && Run(DebugCommandKind.SetPhase, phase: GamePhase.Shop).Success
                && !FindObjectOfType<UIGameplayReadModel>().Current.Night && game.Day == 7 && game.Money == 37, "Returning shop state inconsistent.");
            Require(File.ReadAllBytes(DeveloperToolsVerification.SavePath).SequenceEqual(before), "Scene return overwrote sentinel.");
            Require(service.History.Count > 20 && service.History.Any(r => r.Status == DebugCommandStatus.Unavailable)
                && service.History.Any(r => r.Status == DebugCommandStatus.ConfirmationRequired), "Command results not retained.");
            Debug.Log("DEVELOPER_TOOLS_PHASE1_CHECKS_COMPLETE: live HUD/inventory, day readiness, phase consistency, scene identity, ordinary-save protection, invalid inputs");
        }
    }
}
