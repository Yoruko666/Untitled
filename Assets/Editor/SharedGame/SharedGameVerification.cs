using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Untitled.Editor
{
    // Opt-in development tests, following the project's existing Editor verification pattern.
    public static class SharedGameVerification
    {
        public static string FixturePath => Path.GetFullPath("Library/SharedGameVerification/restart.json");

        private sealed class TestCatalog : IItemCatalog
        {
            public bool TryGetStackLimit(int id, out int limit)
            {
                limit = id == 1 ? 3 : id == 2 ? 2 : 0;
                return limit > 0;
            }
        }

        [MenuItem("Untitled/Shared Game/Development Tests/Run Data Checks")]
        public static void RunDataChecks()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run data checks outside Play Mode.");
            TestQuantitiesAndEvents();
            TestCapacityAndRestore();
            TestStateAndSnapshot();
            VerifySessionProjection();
            VerifySlotIntegration();
            TestSaveErrors();
            WriteRestartFixture();
            Debug.Log("SHARED_GAME_DATA_PASSED (7 groups; restart fixture written)");
        }

        public static void VerifySessionProjection()
        {
            var game = MakeFixture();
            var session = new GameSession(game);
            int events = 0;
            session.Changed += () => events++;
            game.SetDay(5);
            Require(session.Day == 5 && session.Phase == GamePhase.Night && events == 1, "Shared state did not reach session.");
            session.SetProgress(6, GamePhase.Day);
            Require(game.Day == 6 && game.Phase == GamePhase.Shop && events == 2, "Session progress was not atomic/shared.");
            session.SetOrder("current-order", .5f, false);
            session.SetStoryFlag("shared.flag", true);
            Require(game.Order.Id == "current-order" && game.HasStoryFlag("shared.flag"), "Session modules did not reach shared data.");
            var modular = session.Capture();
            Require(modular.Package.Count == 1 && modular.Package[0].Count == 2
                && modular.AcceptedOrders.Count == 1 && modular.CompletedOrders.Count == 1, "Modular capture lost shared modules.");
            var snapshot = game.Capture();
            var restored = new GameData(new TestCatalog());
            Require(restored.TryRestore(snapshot, out _) && restored.Order.Id == "current-order", "Shared capture lost modular order.");
            snapshot.Order.Id = "caller-mutated";
            Require(restored.Order.Id == "current-order" && game.Order.Id == "current-order", "Order snapshot aliases live state.");
            snapshot.Order.Progress = float.NaN;
            Require(!restored.TryRestore(snapshot, out _) && restored.Day == 6, "Invalid order partially restored state.");
            snapshot.Order = null; snapshot.UndergroundFlags = null;
            Require(restored.TryRestore(snapshot, out _) && restored.Order.Id == "", "Original shared-v1 snapshot is incompatible.");
        }

        private sealed class VerificationPhaseRestorer : IGamePhaseRestorer
        {
            public SaveResult Validate(GamePhase phase) => SaveResult.Ok();
            public System.Threading.Tasks.Task EnterAsync(GameSession session) => System.Threading.Tasks.Task.CompletedTask;
        }

        private static void VerifySlotIntegration()
        {
            var storage = new SaveStorage(Path.GetFullPath("Library/SharedGameVerification/modular-slot"));
            var original = new GameSession(MakeFixture());
            original.SetOrder("current-order", .5f, false);
            var saved = original.Capture();
            saved.Underground.Flags.Add("cave.open");
            Require(storage.Save(saved).Success, "Integrated slot save failed.");
            // A reduced capacity must not discard previously saved valid stacks.
            var game = new GameData(new TestCatalog(), 1);
            var session = new GameSession(game);
            var manager = new SaveManager(session, storage, new VerificationPhaseRestorer());
            Require(manager.LoadAsync().GetAwaiter().GetResult().Success && session.IsReady, "Integrated slot restore failed.");
            VerifyFixture(game);
            Require(session.Order.Id == "current-order" && game.UndergroundFlags[0] == "cave.open", "Slot lost modular state.");
            Require(game.InventoryData.GetItemCount(1) == 5 && game.InventoryData.UsedSlots > game.InventoryData.Capacity,
                "Slot restore discarded over-capacity inventory.");
            // Original modular-v1 files have none of these additive fields.
            saved.Package = null; saved.AcceptedOrders = null; saved.CompletedOrders = null;
            Require(storage.Save(saved).Success && manager.LoadAsync().GetAwaiter().GetResult().Success,
                "Original modular-v1 slot is incompatible.");
            Require(game.PackageData.Items.Count == 0 && game.AcceptedOrders.Count == 0 && game.CompletedOrders.Count == 0,
                "Legacy slot retained stale shared modules.");
        }

        private static void TestQuantitiesAndEvents()
        {
            var inventory = new InventoryData(new TestCatalog());
            int changes = 0;
            inventory.Changed += () => changes++;
            Require(inventory.TryAddItem(1, 4) && inventory.TryAddItem(1, 2), "Add/merge failed.");
            Require(inventory.GetItemCount(1) == 6 && inventory.Items.Count == 1 && inventory.UsedSlots == 2, "Stack count failed.");
            Require(inventory.ConsumeItem(1, 2) && inventory.GetItemCount(1) == 4, "Consume failed.");
            foreach (int invalid in new[] { 0, -1 })
            {
                Require(!inventory.TryAddItem(1, invalid) && !inventory.ConsumeItem(1, invalid), "Invalid quantity mutated inventory.");
                Require(!inventory.HasItem(1, invalid), "Invalid query succeeded.");
            }
            Require(!inventory.TryAddItem(999, 1) && !inventory.TryAddItem(0, 1) && !inventory.TryAddItem(-1, 1), "Unknown ID accepted.");
            Require(!inventory.ConsumeItem(1, 5) && !inventory.ConsumeItem(2, 1), "Insufficient items consumed.");
            Require(changes == 3, "Rejected changes fired events.");
            Require(inventory.ConsumeItem(1, 4) && inventory.Items.Count == 0 && changes == 4, "Zero-count removal failed.");
            inventory.Clear(); Require(changes == 4, "Empty clear fired an event.");
            Require(inventory.TryAddItem(1, int.MaxValue) && !inventory.TryAddItem(1, 1), "Addition overflow was not rejected.");
            Require(inventory.UsedSlots == 715827883L, "Slot calculation overflowed.");
            inventory.Clear(); Require(changes == 6, "Clear did not publish exactly once.");
            var package = new PackageData(new TestCatalog());
            package.AddItem(1, 2); Require(package.ConsumeItem(1, 1) && package.GetItemCount(1) == 1, "Legacy package API broke.");
        }

        private static void TestCapacityAndRestore()
        {
            var inventory = new InventoryData(new TestCatalog(), 2);
            Require(inventory.TryAddItem(1, 6) && !inventory.TryAddItem(1, 1) && !inventory.TryAddItem(2, 1), "Capacity did not reject the entire add.");
            Require(inventory.GetItemCount(1) == 6, "Failed add changed quantities.");
            inventory.ConsumeItem(1, 3);
            Require(inventory.TryAddItem(2, 2) && inventory.UsedSlots == 2, "Consume did not free a slot.");
            var saved = new List<ItemStackSaveData>
            {
                new ItemStackSaveData { ItemId = 1, Count = 4 }, new ItemStackSaveData { ItemId = 1, Count = 3 },
                new ItemStackSaveData { ItemId = 999, Count = 1 }, null,
                new ItemStackSaveData { ItemId = 2, Count = -1 }, new ItemStackSaveData { ItemId = 1, Count = int.MaxValue }
            };
            int events = 0; inventory.Changed += () => events++;
            var originalList = inventory.Items;
            Require(inventory.Restore(saved) == 4 && events == 1, "Restore validation/event count failed.");
            Require(ReferenceEquals(originalList, inventory.Items) && inventory.GetItemCount(1) == 7, "Restore replaced read model or lost valid contents.");
            Require(inventory.UsedSlots == 3 && !inventory.TryAddItem(2, 1), "Over-capacity restore did not preserve contents/block additions.");
            inventory.ConsumeItem(1, 4); Require(inventory.TryAddItem(2, 1), "Over-capacity inventory cannot recover by consuming.");
        }

        private static void TestStateAndSnapshot()
        {
            var game = MakeFixture();
            Require(!game.TryAcceptOrder("order-active") && !game.TryAcceptOrder("order-complete"), "Duplicate order accepted.");
            Require(!game.TryCompleteOrder("unknown") && !game.UnlockAbility("double-jump") && !game.SetStoryFlag("intro.finished"), "Duplicate/missing progression changed.");
            Require(!game.SetDay(0) && !game.SetPhase((GamePhase)999), "Invalid state accepted.");
            Require(!game.TrySpendMoney(0) && !game.TrySpendMoney(86) && !game.TryAddMoney(-1), "Invalid economy operation accepted.");
            var saved = game.Capture();
            var restored = new GameData(new TestCatalog());
            var warehouse = restored.InventoryData;
            Require(restored.TryRestore(saved, out int skipped) && skipped == 0, "State restore failed.");
            Require(ReferenceEquals(warehouse, restored.InventoryData), "Restore replaced warehouse instance.");
            VerifyFixture(restored);
            restored.InventoryData.ConsumeItem(1, 1);
            Require(saved.Inventory[0].Count == 5 && game.InventoryData.GetItemCount(1) == 5, "Snapshot shares mutable contents.");
            int coins = restored.Money;
            saved.Version = 999; saved.Money = 1;
            Require(!restored.TryRestore(saved, out _) && restored.Money == coins, "Rejected restore partially changed state.");
            var max = new GameData(); max.AddMoney(int.MaxValue);
            Require(!max.TryAddMoney(1) && max.Money == int.MaxValue, "Money overflow changed state.");
        }

        private static void TestSaveErrors()
        {
            string path = Path.GetFullPath("Library/SharedGameVerification/errors.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            try
            {
                var store = new GameSaveStore(path);
                Require(store.TrySave(MakeFixture().Capture(), out _), "Save failed.");
                Require(store.TrySave(MakeFixture().Capture(), out _), "Atomic overwrite failed.");
                Require(!store.TrySave(null, out _), "Null state overwrote a valid save.");
                Require(store.TryLoad(out var saved, out _) && new GameData(new TestCatalog()).TryRestore(saved, out _), "JSON load failed.");
                File.WriteAllText(path, "{ invalid json");
                Require(!store.TryLoad(out _, out _), "Malformed JSON loaded.");
                File.WriteAllText(path, "{}");
                Require(store.TryLoad(out saved, out _) && !new GameData().TryRestore(saved, out _), "Missing schema fields accepted.");
                File.Delete(path);
                Require(!store.TryLoad(out _, out _), "Missing save loaded.");
                Require(!new GameSaveStore(path + "\0").TrySave(MakeFixture().Capture(), out _), "Invalid file path succeeded.");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        public static void WriteRestartFixture()
        {
            Require(new GameSaveStore(FixturePath).TrySave(MakeFixture().Capture(), out string error), "Restart fixture save failed: " + error);
        }

        // Invoked in a second Unity process to prove file-based recovery after an Editor restart.
        public static void VerifyRestartFixture()
        {
            var store = new GameSaveStore(FixturePath);
            Require(store.TryLoad(out var saved, out string error), "Restart fixture load failed: " + error);
            var game = new GameData(new TestCatalog());
            Require(game.TryRestore(saved, out int skipped) && skipped == 0, "Restart restore rejected fixture.");
            VerifyFixture(game);
            Debug.Log("SHARED_GAME_RESTART_PASSED (fresh process)");
        }

        public static void RunRestartAndDataChecks()
        {
            VerifyRestartFixture();
            RunDataChecks();
        }

        private static GameData MakeFixture()
        {
            var game = new GameData(new TestCatalog());
            game.SetDay(4); game.SetPhase(GamePhase.Underground);
            game.AddMoney(100); game.TrySpendMoney(15);
            game.InventoryData.TryAddItem(1, 5); game.PackageData.TryAddItem(2, 2);
            game.TryAcceptOrder("order-active"); game.TryAcceptOrder("order-complete"); game.TryCompleteOrder("order-complete");
            game.UnlockAbility("double-jump"); game.SetStoryFlag("intro.finished");
            return game;
        }

        private static void VerifyFixture(GameData game)
        {
            Require(game.Day == 4 && game.Phase == GamePhase.Underground && game.Money == 85, "Day/phase/money did not recover.");
            Require(game.InventoryData.GetItemCount(1) == 5 && game.PackageData.GetItemCount(2) == 2, "Items did not recover.");
            Require(game.AcceptedOrders.Count == 1 && game.AcceptedOrders[0] == "order-active"
                && game.CompletedOrders.Count == 1 && game.CompletedOrders[0] == "order-complete", "Order IDs did not recover.");
            Require(game.UnlockedAbilities.Count == 1 && game.UnlockedAbilities[0] == "double-jump" && game.HasStoryFlag("intro.finished"), "Progression did not recover.");
        }

        public static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[SharedGameVerification] " + message);
        }
    }
}
