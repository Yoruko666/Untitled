using System;
using System.Collections.Generic;

namespace Untitled
{
    /// <summary>Save/scene lifecycle over the single shared GameData instance.</summary>
    public sealed class GameSession
    {
        public GameData GameData { get; }
        public int Day => GameData.Day;
        public GamePhase Phase => GameData.Phase;
        public bool IsReady { get; private set; } = true;
        public OrderSaveData Order => GameData.Order;
        public IReadOnlyList<string> StoryFlags => GameData.StoryFlags;
        public event Action Changed;

        public GameSession(GameData gameData)
        {
            GameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            GameData.StateChanged += OnStateChanged;
        }

        private void OnStateChanged() { if (IsReady) Changed?.Invoke(); }

        public void SetProgress(int day, GamePhase phase)
        {
            if (day < 1 || day > 7 || !Enum.IsDefined(typeof(GamePhase), phase)) throw new ArgumentOutOfRangeException(nameof(day));
            GameData.SetProgress(day, phase);
        }

        public void SetOrder(string id, float progress, bool completed)
        {
            if (!GameData.SetOrder(id, progress, completed)) throw new ArgumentException("Invalid order.");
        }

        public void SetStoryFlag(string id, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 128) throw new ArgumentException("Invalid story ID.", nameof(id));
            if (enabled) GameData.SetStoryFlag(id);
            else GameData.RemoveStoryFlag(id);
        }

        public SaveData Capture()
        {
            var data = SaveData.NewGame();
            data.Progress.Day = Day; data.Progress.Phase = Phase;
            data.Economy.Coins = GameData.Money;
            foreach (var item in GameData.InventoryData.Items)
                data.Inventory.Items.Add(item == null ? null : new InventoryItemSaveData { Id = item.Id, Quantity = item.Num });
            data.Order = Order;
            data.Story.Flags.AddRange(GameData.StoryFlags);
            data.Unlocks.AbilityIds.AddRange(GameData.UnlockedAbilities);
            data.Underground.Flags.AddRange(GameData.UndergroundFlags);
            data.Package = GameData.PackageData.Capture();
            data.AcceptedOrders = new List<string>(GameData.AcceptedOrders);
            data.CompletedOrders = new List<string>(GameData.CompletedOrders);
            return data;
        }

        internal void Restore(SaveData data)
        {
            var result = SaveValidator.Validate(data);
            if (!result.Success) throw new ArgumentException(result.Message);
            var shared = new GameStateSaveData
            {
                Version = GameStateSaveData.CurrentVersion, Day = data.Progress.Day, Phase = data.Progress.Phase,
                Money = data.Economy.Coins, Inventory = new List<ItemStackSaveData>(), Package = data.Package,
                AcceptedOrders = data.AcceptedOrders, CompletedOrders = data.CompletedOrders,
                Order = data.Order, StoryFlags = data.Story.Flags, UnlockedAbilities = data.Unlocks.AbilityIds,
                UndergroundFlags = data.Underground.Flags
            };
            foreach (var item in data.Inventory.Items)
                shared.Inventory.Add(new ItemStackSaveData { ItemId = item.Id, Count = item.Quantity });
            IsReady = false;
            // Retain valid stacks even after a capacity reduction.
            if (!GameData.TryRestore(shared, out int skipped)) throw new ArgumentException("Invalid shared state.");
            if (skipped > 0) UnityEngine.Debug.LogWarning($"[GameSession] 恢复时跳过 {skipped} 条无效物品记录。");
        }

        internal void CompleteRestore() { IsReady = true; Changed?.Invoke(); }
    }
}
