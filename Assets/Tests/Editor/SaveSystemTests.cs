using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Untitled.Tests
{
    public sealed class SaveSystemTests
    {
        private string directory;
        private SaveStorage storage;
        private GameSession session;
        private SaveManager manager;
        private PhaseHost phase;

        private sealed class PhaseHost : IGamePhaseRestorer
        {
            public Action<GameSession> OnEnter;
            public SaveResult Preflight = SaveResult.Ok();
            public TaskCompletionSource<bool> Pending;
            public SaveResult Validate(GamePhase value) => Preflight;
            public Task EnterAsync(GameSession value)
            {
                Assert.IsFalse(value.IsReady, "UI publication must wait for scene initialization.");
                OnEnter?.Invoke(value);
                return Pending?.Task ?? Task.CompletedTask;
            }
        }

        [Serializable]
        private sealed class TestEnvelope { public int FormatVersion; public string Payload; public string Sha256; }

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "UntitledSaveTests", Guid.NewGuid().ToString("N"));
            storage = new SaveStorage(directory);
            session = new GameSession(new GameData());
            phase = new PhaseHost();
            manager = new SaveManager(session, storage, phase);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void NewGameInitializesModulesWithoutWritingSlot()
        {
            session.GameData.AddMoney(20); session.GameData.InventoryData.AddItem(1, 2);
            session.SetProgress(7, GamePhase.Night); session.SetStoryFlag("ending", true);
            Assert.IsTrue(manager.NewGameAsync().Result.Success);
            Assert.AreEqual(1, session.Day); Assert.AreEqual(GamePhase.Day, session.Phase);
            Assert.AreEqual(0, session.GameData.Money); Assert.IsEmpty(session.GameData.InventoryData.Items);
            Assert.IsEmpty(session.StoryFlags); Assert.IsTrue(session.IsReady);
            Assert.IsFalse(File.Exists(storage.MainPath));
        }

        [Test]
        public void SaveLoadRestoresMoneyItemsDayOrderAndStoryBeforePublication()
        {
            session.SetProgress(2, GamePhase.Day);
            session.GameData.AddMoney(128); session.GameData.InventoryData.AddItem(101, 6);
            session.SetOrder("day2.order.01", 1, true); session.SetStoryFlag("intro.finished", true);
            Assert.IsTrue(manager.Save().Success);
            var game = session.GameData; var inventory = game.InventoryData; var list = inventory.Items;
            game.TrySpendMoney(100); inventory.Clear(); session.SetProgress(1, GamePhase.Night);
            session.SetOrder("", 0, false); session.SetStoryFlag("intro.finished", false);
            bool entered = false; int notifications = 0;
            phase.OnEnter = restored =>
            {
                Assert.AreEqual(2, restored.Day); Assert.AreEqual(128, restored.GameData.Money);
                Assert.AreEqual(6, restored.GameData.InventoryData.GetItemCount(101));
                entered = true;
            };
            session.Changed += () => { Assert.IsTrue(entered); Assert.IsTrue(session.IsReady); notifications++; };
            Assert.IsTrue(manager.LoadAsync().Result.Success);
            Assert.AreSame(game, session.GameData); Assert.AreSame(inventory, game.InventoryData); Assert.AreSame(list, inventory.Items);
            Assert.IsTrue(session.Order.Completed); Assert.AreEqual("day2.order.01", session.Order.Id);
            CollectionAssert.Contains(session.StoryFlags, "intro.finished"); Assert.AreEqual(1, notifications);
            // Verify the warehouse index is rebuilt, not just its public list.
            Assert.IsTrue(inventory.ConsumeItem(101, 2)); Assert.AreEqual(4, inventory.GetItemCount(101));
        }

        [Test]
        public void DayOneToDayTwoPersistsExplicitProgress()
        {
            Assert.AreEqual(1, session.Day);
            session.SetProgress(2, GamePhase.Day); Assert.IsTrue(manager.Save().Success);
            var fresh = new GameSession(new GameData());
            Assert.IsTrue(new SaveManager(fresh, storage, new PhaseHost()).LoadAsync().Result.Success);
            Assert.AreEqual(2, fresh.Day);
        }

        [Test]
        public void SceneReplacementKeepsSharedSessionAndState()
        {
            // Preview scenes are isolated even when Test Runner has an unsaved scene open.
            var first = EditorSceneManager.NewPreviewScene();
            var second = EditorSceneManager.NewPreviewScene();
            try
            {
                session.GameData.AddMoney(12); session.GameData.InventoryData.AddItem(1, 3);
                session.SetOrder("shared.order", .5f, false); session.SetProgress(1, GamePhase.Night);
                Assert.IsTrue(manager.Save().Success);
                phase.OnEnter = _ => EditorSceneManager.ClosePreviewScene(first);
                Assert.IsTrue(manager.LoadAsync().Result.Success);
                Assert.IsFalse(first.isLoaded); Assert.IsTrue(second.isLoaded);
                Assert.AreEqual(12, session.GameData.Money); Assert.AreEqual(3, session.GameData.InventoryData.GetItemCount(1));
                Assert.AreEqual(.5f, session.Order.Progress);
            }
            finally
            {
                if (first.isLoaded) EditorSceneManager.ClosePreviewScene(first);
                EditorSceneManager.ClosePreviewScene(second);
            }
        }

        [Test]
        public void SnapshotIsIndependentOfRuntimeAndCaller()
        {
            session.GameData.InventoryData.AddItem(1, 4); session.SetOrder("order", .25f, false);
            var data = session.Capture(); data.Inventory.Items[0].Quantity = 999; data.Order.Id = "other";
            Assert.AreEqual(4, session.GameData.InventoryData.GetItemCount(1)); Assert.AreEqual("order", session.Order.Id);
        }

        [Test]
        public void SceneFailureRollsBackRuntimeAndDoesNotModifySlot()
        {
            session.GameData.AddMoney(10); Assert.IsTrue(manager.Save().Success);
            session.GameData.AddMoney(7);
            string before = File.ReadAllText(storage.MainPath);
            phase.OnEnter = _ => throw new InvalidOperationException("Simulated scene failure.");
            Assert.AreEqual(SaveError.PhaseUnavailable, manager.LoadAsync().Result.Error);
            Assert.AreEqual(17, session.GameData.Money); Assert.IsTrue(session.IsReady);
            Assert.AreEqual(before, File.ReadAllText(storage.MainPath));
        }

        [Test]
        public void UnavailablePhaseLeavesRuntimeUntouched()
        {
            Assert.IsTrue(manager.Save().Success); session.GameData.AddMoney(7);
            phase.Preflight = SaveResult.Fail(SaveError.PhaseUnavailable, "Missing phase scene.");
            Assert.AreEqual(SaveError.PhaseUnavailable, manager.LoadAsync().Result.Error);
            Assert.AreEqual(7, session.GameData.Money);
        }

        [Test]
        public void PendingRestoreBlocksConcurrentOperationsAndDefaultUiReads()
        {
            Assert.IsTrue(manager.Save().Success);
            phase.Pending = new TaskCompletionSource<bool>();
            var load = manager.LoadAsync();
            Assert.IsFalse(session.IsReady); Assert.IsFalse(load.IsCompleted);
            Assert.AreEqual(SaveError.Busy, manager.Save().Error);
            Assert.AreEqual(SaveError.Busy, manager.LoadAsync().Result.Error);
            phase.Pending.SetResult(true);
            Assert.IsTrue(load.Result.Success); Assert.IsTrue(session.IsReady);
        }

        [Test]
        public void CorruptPrimaryUsesBackupAndNextSavePreservesBackup()
        {
            session.GameData.AddMoney(11); Assert.IsTrue(manager.Save().Success);
            session.GameData.AddMoney(22); Assert.IsTrue(manager.Save().Success);
            string backup = File.ReadAllText(storage.BackupPath);
            File.WriteAllText(storage.MainPath, "broken json");
            var result = manager.LoadAsync().Result;
            Assert.IsTrue(result.Success); Assert.IsTrue(result.RecoveredFromBackup);
            Assert.AreEqual(11, session.GameData.Money); Assert.AreEqual("broken json", File.ReadAllText(storage.MainPath));
            Assert.IsTrue(manager.Save().Success); Assert.AreEqual(backup, File.ReadAllText(storage.BackupPath));
            Assert.IsTrue(storage.Load(out _).Success);
        }

        [Test]
        public void MissingPrimaryCanRecoverBackup()
        {
            Assert.IsTrue(manager.Save().Success); Assert.IsTrue(manager.Save().Success);
            File.Delete(storage.MainPath);
            Assert.IsTrue(storage.Load(out _).RecoveredFromBackup);
        }

        [Test]
        public void MissingSlotDoesNotResetOrCreateState()
        {
            session.GameData.AddMoney(9);
            Assert.AreEqual(SaveError.NotFound, manager.LoadAsync().Result.Error);
            Assert.AreEqual(9, session.GameData.Money); Assert.IsFalse(Directory.Exists(directory));
        }

        [Test]
        public void ChecksumDetectsValidJsonTampering()
        {
            Assert.IsTrue(manager.Save().Success);
            var envelope = JsonUtility.FromJson<TestEnvelope>(File.ReadAllText(storage.MainPath));
            envelope.Payload = envelope.Payload.Replace("\"Coins\":0", "\"Coins\":100");
            File.WriteAllText(storage.MainPath, JsonUtility.ToJson(envelope));
            Assert.AreEqual(SaveError.Corrupt, storage.Load(out _).Error);
        }

        [TestCase(0)]
        [TestCase(2)]
        public void UnsupportedVersionIsNotOverwrittenOrSilentlyRolledBack(int version)
        {
            Assert.IsTrue(manager.Save().Success); Assert.IsTrue(manager.Save().Success);
            var data = SaveData.NewGame(); data.SchemaVersion = version;
            WriteFixture(data);
            string before = File.ReadAllText(storage.MainPath);
            Assert.AreEqual(SaveError.UnsupportedVersion, manager.LoadAsync().Result.Error);
            Assert.AreEqual(SaveError.UnsupportedVersion, manager.Save().Error);
            Assert.AreEqual(before, File.ReadAllText(storage.MainPath));
        }

        [Test]
        public void InvalidInputNeverReplacesValidSave()
        {
            Assert.IsTrue(manager.Save().Success); string before = File.ReadAllText(storage.MainPath);
            session.GameData.Money = -1;
            Assert.AreEqual(SaveError.InvalidData, manager.Save().Error);
            Assert.AreEqual(before, File.ReadAllText(storage.MainPath));
        }

        [Test]
        public void RejectsInvalidModulesIdsAndQuantities()
        {
            var cases = new Action<SaveData>[]
            {
                d => d.Progress.Day = 8, d => d.Progress.Phase = (GamePhase)99,
                d => d.Inventory.Items.Add(new InventoryItemSaveData { Id = 1, Quantity = 0 }),
                d => { d.Inventory.Items.Add(new InventoryItemSaveData { Id = 1, Quantity = 1 }); d.Inventory.Items.Add(new InventoryItemSaveData { Id = 1, Quantity = 2 }); },
                d => d.Story.Flags.Add(""), d => { d.Story.Flags.Add("flag"); d.Story.Flags.Add("flag"); },
                d => d.Order.Progress = float.NaN, d => d.Order.Completed = true,
                d => d.Economy = null, d => d.Inventory.Items = null
            };
            foreach (var change in cases)
            { var data = SaveData.NewGame(); change(data); Assert.AreEqual(SaveError.InvalidData, SaveValidator.Validate(data).Error); }
        }

        [Test]
        public void BothCorruptCopiesArePreservedAndRuntimeRemainsUntouched()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(storage.MainPath, "main evidence"); File.WriteAllText(storage.BackupPath, "backup evidence");
            session.GameData.AddMoney(19);
            Assert.IsFalse(manager.LoadAsync().Result.Success); Assert.IsFalse(manager.Save().Success);
            Assert.AreEqual(19, session.GameData.Money);
            Assert.AreEqual("main evidence", File.ReadAllText(storage.MainPath)); Assert.AreEqual("backup evidence", File.ReadAllText(storage.BackupPath));
        }

        [Test]
        public void UnwritablePathReturnsIoError()
        {
            Directory.CreateDirectory(directory); string blocked = Path.Combine(directory, "file");
            File.WriteAllText(blocked, "path blocker");
            var inaccessible = new SaveStorage(blocked);
            Assert.AreEqual(SaveError.Io, inaccessible.Save(SaveData.NewGame()).Error);
            Assert.AreEqual("path blocker", File.ReadAllText(blocked));
        }

        [Test]
        public void ReplacementFailurePreservesLastValidCopies()
        {
            session.GameData.AddMoney(1); Assert.IsTrue(manager.Save().Success);
            session.GameData.AddMoney(1); Assert.IsTrue(manager.Save().Success);
            string main = File.ReadAllText(storage.MainPath); string backup = File.ReadAllText(storage.BackupPath);
            session.GameData.AddMoney(1);
            using (var locked = new FileStream(storage.MainPath, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.AreEqual(SaveError.Io, manager.Save().Error);
            Assert.AreEqual(main, File.ReadAllText(storage.MainPath)); Assert.AreEqual(backup, File.ReadAllText(storage.BackupPath));
            Assert.IsEmpty(Directory.GetFiles(directory, "*.tmp"));
        }

        [Test]
        public void LockedBackupPreventsReplacementWithoutDeletingMain()
        {
            Assert.IsTrue(manager.Save().Success); Assert.IsTrue(manager.Save().Success);
            string main = File.ReadAllText(storage.MainPath); string backup = File.ReadAllText(storage.BackupPath);
            session.GameData.AddMoney(8);
            using (var locked = new FileStream(storage.BackupPath, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.AreEqual(SaveError.Io, manager.Save().Error);
            Assert.AreEqual(main, File.ReadAllText(storage.MainPath)); Assert.AreEqual(backup, File.ReadAllText(storage.BackupPath));
            Assert.IsEmpty(Directory.GetFiles(directory, "*.tmp"));
        }

        private void WriteFixture(SaveData data)
        {
            string payload = JsonUtility.ToJson(data);
            string checksum;
            using (var hash = SHA256.Create()) checksum = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            File.WriteAllText(storage.MainPath, JsonUtility.ToJson(new TestEnvelope { FormatVersion = 1, Payload = payload, Sha256 = checksum }));
        }
    }
}
