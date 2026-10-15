using System;
using System.Collections.Generic;

namespace Untitled
{
    public class GameData
    {
        private readonly List<string> acceptedOrders = new List<string>();
        private readonly List<string> completedOrders = new List<string>();
        private readonly List<string> unlockedAbilities = new List<string>();
        private readonly List<string> storyFlags = new List<string>();
        private readonly List<string> undergroundFlags = new List<string>();
        private OrderSaveData order = new OrderSaveData { Id = "" };

        public int Day { get; private set; } = 1;
        public GamePhase Phase { get; private set; } = GamePhase.Shop;
        public int Money { get; private set; }
        public InventoryData InventoryData { get; }
        public PackageData PackageData { get; }
        public IReadOnlyList<string> AcceptedOrders { get; }
        public IReadOnlyList<string> CompletedOrders { get; }
        public IReadOnlyList<string> UnlockedAbilities { get; }
        public IReadOnlyList<string> StoryFlags { get; }
        public IReadOnlyList<string> UndergroundFlags { get; }
        public OrderSaveData Order => new OrderSaveData { Id = order.Id, Progress = order.Progress, Completed = order.Completed };
        public event Action<int> MoneyChanged;
        public event Action StateChanged;

        public GameData(IItemCatalog catalog = null, int inventoryCapacity = 0)
        {
            InventoryData = new InventoryData(catalog, inventoryCapacity);
            PackageData = new PackageData(catalog);
            AcceptedOrders = acceptedOrders.AsReadOnly();
            CompletedOrders = completedOrders.AsReadOnly();
            UnlockedAbilities = unlockedAbilities.AsReadOnly();
            StoryFlags = storyFlags.AsReadOnly();
            UndergroundFlags = undergroundFlags.AsReadOnly();
        }

        public void AddMoney(int amount) => TryAddMoney(amount);

        public bool TryAddMoney(int amount)
        {
            if (amount <= 0 || (long)Money + amount > int.MaxValue) return false;
            Money += amount;
            MoneyChanged?.Invoke(Money);
            StateChanged?.Invoke();
            return true;
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount <= 0 || Money < amount) return false;
            Money -= amount;
            MoneyChanged?.Invoke(Money);
            StateChanged?.Invoke();
            return true;
        }

        public bool SetDay(int value)
        {
            if (value < 1) return false;
            if (Day == value) return true;
            Day = value;
            StateChanged?.Invoke();
            return true;
        }

        public bool SetPhase(GamePhase value)
        {
            if (!Enum.IsDefined(typeof(GamePhase), value)) return false;
            if (Phase == value) return true;
            Phase = value;
            StateChanged?.Invoke();
            return true;
        }

        internal void SetProgress(int day, GamePhase phase)
        {
            if (Day == day && Phase == phase) return;
            Day = day;
            Phase = phase;
            StateChanged?.Invoke();
        }

        // IDs identify order instances. Recipe and fulfillment rules belong to the future order service.
        public bool TryAcceptOrder(string id)
        {
            if (!ValidId(id) || acceptedOrders.Contains(id) || completedOrders.Contains(id)) return false;
            acceptedOrders.Add(id);
            StateChanged?.Invoke();
            return true;
        }

        public bool TryCompleteOrder(string id)
        {
            if (!ValidId(id) || !acceptedOrders.Remove(id)) return false;
            completedOrders.Add(id);
            StateChanged?.Invoke();
            return true;
        }

        public bool UnlockAbility(string id) => AddId(unlockedAbilities, id);
        public bool SetStoryFlag(string id) => AddId(storyFlags, id);
        public bool HasStoryFlag(string id) => ValidId(id) && storyFlags.Contains(id);

        public bool RemoveStoryFlag(string id)
        {
            if (!storyFlags.Remove(id)) return false;
            StateChanged?.Invoke();
            return true;
        }

        public bool SetOrder(string id, float progress, bool completed)
        {
            var candidate = new OrderSaveData { Id = id, Progress = progress, Completed = completed };
            if (!SaveValidator.ValidOrder(candidate)) return false;
            order = candidate;
            StateChanged?.Invoke();
            return true;
        }

        public GameStateSaveData Capture()
        {
            return new GameStateSaveData
            {
                Version = GameStateSaveData.CurrentVersion, Day = Day, Phase = Phase, Money = Money,
                Inventory = InventoryData.Capture(), Package = PackageData.Capture(),
                AcceptedOrders = new List<string>(acceptedOrders), CompletedOrders = new List<string>(completedOrders),
                UnlockedAbilities = new List<string>(unlockedAbilities), StoryFlags = new List<string>(storyFlags),
                Order = Order, UndergroundFlags = new List<string>(undergroundFlags)
            };
        }

        public bool TryRestore(GameStateSaveData saved, out int skippedItems)
        {
            skippedItems = 0;
            if (saved == null || saved.Version != GameStateSaveData.CurrentVersion || saved.Day < 1
                || saved.Money < 0 || !Enum.IsDefined(typeof(GamePhase), saved.Phase)
                || (saved.Order != null && !SaveValidator.ValidOrder(saved.Order))) return false;

            Day = saved.Day;
            Phase = saved.Phase;
            Money = saved.Money;
            CopyIds(saved.CompletedOrders, completedOrders);
            CopyIds(saved.AcceptedOrders, acceptedOrders);
            acceptedOrders.RemoveAll(completedOrders.Contains);
            CopyIds(saved.UnlockedAbilities, unlockedAbilities);
            CopyIds(saved.StoryFlags, storyFlags);
            CopyIds(saved.UndergroundFlags, undergroundFlags);
            order = saved.Order == null ? new OrderSaveData { Id = "" }
                : new OrderSaveData { Id = saved.Order.Id, Progress = saved.Order.Progress, Completed = saved.Order.Completed };
            skippedItems = InventoryData.Restore(saved.Inventory) + PackageData.Restore(saved.Package);
            MoneyChanged?.Invoke(Money);
            StateChanged?.Invoke();
            return true;
        }

        private bool AddId(List<string> target, string id)
        {
            if (!ValidId(id) || target.Contains(id)) return false;
            target.Add(id);
            StateChanged?.Invoke();
            return true;
        }

        private static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id);
        private static void CopyIds(IEnumerable<string> source, List<string> target)
        {
            target.Clear();
            if (source == null) return;
            foreach (string id in source) if (ValidId(id) && !target.Contains(id)) target.Add(id);
        }
    }
}
