using System;
using System.Collections.Generic;

namespace Untitled
{
    public enum GamePhase
    {
        Shop = 0,
        Underground = 1,
        Settlement = 2,
        Day = Shop,
        Night = Underground
    }

    [Serializable]
    public sealed class ItemStackSaveData
    {
        public int ItemId;
        public int Count;
    }

    // DTO only: no scene, UI, configuration or runtime service references.
    [Serializable]
    public sealed class GameStateSaveData
    {
        public const int CurrentVersion = 1;
        public int Version;
        public int Day;
        public GamePhase Phase;
        public int Money;
        public List<ItemStackSaveData> Inventory;
        public List<ItemStackSaveData> Package;
        public List<string> AcceptedOrders;
        public List<string> CompletedOrders;
        public List<string> UnlockedAbilities;
        public List<string> StoryFlags;
        public OrderSaveData Order;
        public List<string> UndergroundFlags;
    }
}
