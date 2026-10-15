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
    [InitializeOnLoad]
    public static class SharedGamePlayVerification
    {
        private const string Pending = "Untitled.SharedGame.PlayVerification";
        private const string Restart = "Untitled.SharedGame.RestartVerification";
        public static string SavePath => Path.GetFullPath("Library/SharedGameVerification/play-session.json");
        static SharedGamePlayVerification() => EditorApplication.playModeStateChanged += OnState;

        [MenuItem("Untitled/Shared Game/Development Tests/Run Shop Integration Checks")]
        public static void RunPlayChecks() => Begin(false);

        [MenuItem("Untitled/Shared Game/Development Tests/Verify Saved Session After Restart")]
        public static void RunRestartChecks() => Begin(true);

        private static void Begin(bool restart)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Start verification outside Play Mode.");
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save your current scene before running verification.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Pending, true);
            SessionState.SetBool(Restart, restart);
            EditorApplication.isPlaying = true;
        }

        private static void OnState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            var go = new GameObject("DEVELOPMENT ONLY - Shared Game Verification");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<SharedGamePlayVerificationRunner>().Restart = SessionState.GetBool(Restart, false);
        }
    }

    public sealed class SharedGamePlayVerificationRunner : MonoBehaviour
    {
        public bool Restart;
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
                catch (Exception exception) { Debug.LogException(exception); passed = false; break; }
                yield return next;
            }
            Application.logMessageReceived -= OnLog;
            passed &= errors == 0;
            Directory.CreateDirectory("Library/SharedGameVerification");
            File.WriteAllText("Library/SharedGameVerification/" + (Restart ? "restart" : "integration") + ".txt",
                (passed ? "PASSED" : "FAILED") + "\nErrors: " + errors + "\n");
            Debug.Log((Restart ? "SHARED_GAME_MANAGER_RESTART_" : "SHARED_GAME_SHOP_INTEGRATION_") + (passed ? "PASSED" : "FAILED"));
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 1);
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }

        private IEnumerator Checks()
        {
            // Initialize only the existing database/game services, with an isolated test save.
            var database = new DatabaseManager(); database.Initialize();
            int itemId = database.ItemDatabase.All.Keys.First();
            TestStackConfiguration(database.ItemDatabase, itemId);
            if (!Restart && File.Exists(SharedGamePlayVerification.SavePath)) File.Delete(SharedGamePlayVerification.SavePath);
            var manager = new GameManager(); manager.Configure(null, SharedGamePlayVerification.SavePath); manager.Initialize();
            var game = manager.GameData;
            if (Restart)
            {
                CheckSession(game, itemId);
                var shopLoad = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Bussiness.unity", new LoadSceneParameters(LoadSceneMode.Single));
                while (!shopLoad.isDone) yield return null;
                yield return null; yield return null;
                var restoredHUD = FindObjectOfType<UIGameplayReadModel>();
                SharedGameVerification.Require(restoredHUD.Current.Day == 4 && restoredHUD.Current.Coins == 37, "Startup restore was not projected to HUD.");
                yield break;
            }

            game.SetDay(4); game.SetPhase(GamePhase.Shop); game.AddMoney(37);
            game.TryAcceptOrder("accepted-instance"); game.TryAcceptOrder("completed-instance"); game.TryCompleteOrder("completed-instance");
            game.UnlockAbility("test.ability"); game.SetStoryFlag("test.story");
            SharedGameVerification.Require(game.InventoryData.TryAddItem(itemId, 4), "Real item table ID rejected.");
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Bussiness.unity", new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone) yield return null;
            yield return null; yield return null;
            var source = FindObjectOfType<UIGameplayReadModel>();
            var shop = FindObjectOfType<ShopSceneBindings>();
            SharedGameVerification.Require(shop != null && shop.Player != null && source != null, "Existing shop entry/player/HUD missing.");
            SharedGameVerification.Require(source.Current.Day == 4 && source.Current.Coins == 37, "HUD failed to read shared state.");
            game.SetDay(5); SharedGameVerification.Require(source.Current.Day == 5, "Day event did not refresh HUD."); game.SetDay(4);
            shop.Stations.First(s => s.Kind == ShopStationKind.Storage).Interact();
            yield return null;
            var window = UIManager.Instance.Top as UIInventoryWindow;
            SharedGameVerification.Require(window != null && window.View.Data.Items.Single().Quantity == 4, "Storage did not open real inventory.");
            SharedGameVerification.Require(game.InventoryData.TryAddItem(itemId, 2), "Add failed while window open.");
            SharedGameVerification.Require(window.View.Data.Items.Single().Quantity == 6
                && window.View.Slots.First().Data.Quantity == 6, "Added items did not update the open UI/slot.");
            SharedGameVerification.Require(game.InventoryData.ConsumeItem(itemId, 3) && window.View.Data.Items.Single().Quantity == 3,
                "Consumption did not update the open UI.");
            SharedGameVerification.Require(!game.InventoryData.TryAddItem(int.MaxValue, 1)
                && window.View.Data.Items.Single().Quantity == 3, "Invalid ID altered UI.");
            SharedGameVerification.Require(shop.Player.UIBlocked && manager.IsPaused, "Storage modal no longer blocks first person input.");

            SharedGameVerification.Require(manager.TrySave(out string saveError), "Manager save failed: " + saveError);
            var inventory = game.InventoryData;
            game.InventoryData.ConsumeItem(itemId, 1); game.SetDay(9); game.AddMoney(1);
            SharedGameVerification.Require(manager.TryLoad(out string loadError), "Manager load failed: " + loadError);
            CheckSession(game, itemId);
            SharedGameVerification.Require(ReferenceEquals(inventory, game.InventoryData) && window.View.Data.Items.Single().Quantity == 3
                && source.Current.Day == 4 && source.Current.Coins == 37, "Load did not refresh retained UI bindings.");

            UIManager.Instance.CloseAll();
            SharedGameVerification.Require(!shop.Player.UIBlocked && !manager.IsPaused, "Close did not resume shop controls.");
            // Closing removes the window subscription; reopening receives the latest snapshot.
            game.InventoryData.TryAddItem(itemId, 1);
            SharedGameVerification.Require(window.View.Data.Items.Single().Quantity == 3, "Closed inventory window retained a subscription.");
            shop.Stations.First(s => s.Kind == ShopStationKind.Storage).Interact();
            SharedGameVerification.Require(window.View.Data.Items.Single().Quantity == 4, "Reopened inventory is stale.");
            game.InventoryData.ConsumeItem(itemId, 1); UIManager.Instance.CloseAll();
            source.Publish(new UIHUDData(4, 37, false, new UIOrderData("display-order", "测试订单", "客人", "材料", "等待", 0, false), null));
            shop.Stations.First(s => s.Kind == ShopStationKind.Preparation).Interact();
            SharedGameVerification.Require(UIManager.Instance.Top is UIShopWorkPanel work && work.View.Data.Id == "display-order", "Existing preparation/order binding broke.");
            UIManager.Instance.CloseAll();
            UIManager.Instance.Open(UIPanel.Menu);
            SharedGameVerification.Require(manager.IsPaused && shop.Player.UIBlocked, "Existing pause menu broke.");
            UIManager.Instance.CloseAll();

            var adventureLoad = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Adventure.unity", new LoadSceneParameters(LoadSceneMode.Single));
            while (!adventureLoad.isDone) yield return null;
            yield return null; yield return null;
            SharedGameVerification.Require(ReferenceEquals(manager, GameManager.Instance) && ReferenceEquals(game, manager.GameData), "Scene load recreated shared state.");
            CheckSession(game, itemId);
            source = FindObjectOfType<UIGameplayReadModel>();
            SharedGameVerification.Require(source.Current.Night && source.Current.Day == 4 && source.Current.Coins == 37, "Existing night HUD or shared day projection broke.");
            var returnLoad = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Bussiness.unity", new LoadSceneParameters(LoadSceneMode.Single));
            while (!returnLoad.isDone) yield return null;
            yield return null; yield return null;
            CheckSession(game, itemId);
            SharedGameVerification.Require(!FindObjectOfType<UIGameplayReadModel>().Current.Night, "Shop day HUD did not return.");
            SharedGameVerification.Require(manager.TrySave(out saveError), "Restart fixture save failed: " + saveError);
        }

        private static void CheckSession(GameData game, int id)
        {
            SharedGameVerification.Require(game.Day == 4 && game.Phase == GamePhase.Shop && game.Money == 37
                && game.InventoryData.GetItemCount(id) == 3, "Shared day/phase/money/items not preserved.");
            SharedGameVerification.Require(game.AcceptedOrders.Single() == "accepted-instance" && game.CompletedOrders.Single() == "completed-instance"
                && game.UnlockedAbilities.Single() == "test.ability" && game.HasStoryFlag("test.story"), "Shared progression not preserved.");
        }

        private static void TestStackConfiguration(ItemDatabase database, int id)
        {
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            var settings = ScriptableObject.CreateInstance<InventorySettings>();
            try
            {
                var item = new SerializedObject(definition);
                item.FindProperty("itemId").intValue = id;
                item.FindProperty("maxStack").intValue = 3;
                item.ApplyModifiedPropertiesWithoutUndo();
                var config = new SerializedObject(settings);
                config.FindProperty("limitCapacity").boolValue = true;
                config.FindProperty("capacity").intValue = 2;
                config.FindProperty("items").arraySize = 1;
                config.FindProperty("items").GetArrayElementAtIndex(0).objectReferenceValue = definition;
                config.ApplyModifiedPropertiesWithoutUndo();
                var inventory = new InventoryData(new ItemCatalog(database, settings), settings.Capacity);
                SharedGameVerification.Require(inventory.TryAddItem(id, 6) && !inventory.TryAddItem(id, 1) && inventory.UsedSlots == 2,
                    "SO stack/capacity configuration was not applied.");
                item.FindProperty("maxStack").intValue = 1; item.ApplyModifiedPropertiesWithoutUndo();
                SharedGameVerification.Require(inventory.UsedSlots == 2, "Runtime catalog changed with the source SO.");
                config.FindProperty("limitCapacity").boolValue = false; config.ApplyModifiedPropertiesWithoutUndo();
                SharedGameVerification.Require(settings.Capacity == 0, "Capacity cannot be disabled.");
            }
            finally { Destroy(definition); Destroy(settings); }
        }
    }
}
