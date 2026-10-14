using System;
using System.Collections.Generic;

namespace Untitled
{
    /// <summary>Scene-independent state; existing economy and warehouse remain authoritative.</summary>
    public sealed class GameSession
    {
        private SaveData state = SaveData.NewGame();
        public GameData GameData { get; }
        public int Day => state.Progress.Day;
        public GamePhase Phase => state.Progress.Phase;
        public bool IsReady { get; private set; } = true;
        public OrderSaveData Order => new OrderSaveData { Id = state.Order.Id, Progress = state.Order.Progress, Completed = state.Order.Completed };
        public IReadOnlyList<string> StoryFlags => state.Story.Flags.AsReadOnly();
        public event Action Changed;

        public GameSession(GameData gameData) { GameData = gameData ?? throw new ArgumentNullException(nameof(gameData)); }

        public void SetProgress(int day, GamePhase phase)
        {
            if (day < 1 || day > 7 || !Enum.IsDefined(typeof(GamePhase), phase)) throw new ArgumentOutOfRangeException(nameof(day));
            state.Progress.Day = day; state.Progress.Phase = phase;
            if (IsReady) Changed?.Invoke();
        }

        public void SetOrder(string id, float progress, bool completed)
        {
            var candidate = Capture();
            candidate.Order = new OrderSaveData { Id = id, Progress = progress, Completed = completed };
            var result = SaveValidator.Validate(candidate);
            if (!result.Success) throw new ArgumentException(result.Message);
            state.Order = candidate.Order;
            if (IsReady) Changed?.Invoke();
        }

        public void SetStoryFlag(string id, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 128) throw new ArgumentException("Invalid story ID.", nameof(id));
            if (enabled && !state.Story.Flags.Contains(id)) state.Story.Flags.Add(id);
            else if (!enabled) state.Story.Flags.Remove(id);
            if (IsReady) Changed?.Invoke();
        }

        public SaveData Capture()
        {
            var data = SaveData.NewGame();
            data.Progress.Day = Day; data.Progress.Phase = Phase;
            data.Economy.Coins = GameData.Money;
            foreach (var item in GameData.InventoryData.Items)
                data.Inventory.Items.Add(item == null ? null : new InventoryItemSaveData { Id = item.Id, Quantity = item.Num });
            data.Order = Order;
            data.Story.Flags.AddRange(state.Story.Flags);
            data.Unlocks.AbilityIds.AddRange(state.Unlocks.AbilityIds);
            data.Underground.Flags.AddRange(state.Underground.Flags);
            return data;
        }

        internal void Restore(SaveData data)
        {
            var result = SaveValidator.Validate(data);
            if (!result.Success) throw new ArgumentException(result.Message);
            IsReady = false;
            // Copy each module: caller-owned DTOs must never become live state.
            state = SaveData.NewGame();
            state.Progress.Day = data.Progress.Day; state.Progress.Phase = data.Progress.Phase;
            state.Order = new OrderSaveData { Id = data.Order.Id, Progress = data.Order.Progress, Completed = data.Order.Completed };
            state.Story.Flags.AddRange(data.Story.Flags);
            state.Unlocks.AbilityIds.AddRange(data.Unlocks.AbilityIds);
            state.Underground.Flags.AddRange(data.Underground.Flags);
            GameData.InventoryData.Clear();
            foreach (var item in data.Inventory.Items) GameData.InventoryData.AddItem(item.Id, item.Quantity);
            GameData.PackageData.Clear();
            GameData.RestoreMoney(data.Economy.Coins);
        }

        internal void CompleteRestore() { IsReady = true; Changed?.Invoke(); }
    }
}
