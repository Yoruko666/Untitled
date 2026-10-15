using System;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentSchemaVersion = 1;
        [SerializeField] private int schemaVersion;
        public int SchemaVersion { get => schemaVersion; set => schemaVersion = value; }
        public ProgressSaveData Progress;
        public EconomySaveData Economy;
        public InventorySaveData Inventory;
        public OrderSaveData Order;
        public StorySaveData Story;
        public UnlockSaveData Unlocks;
        public UndergroundSaveData Underground;
        public List<ItemStackSaveData> Package;
        public List<string> AcceptedOrders;
        public List<string> CompletedOrders;

        public static SaveData NewGame() => new SaveData
        {
            SchemaVersion = CurrentSchemaVersion,
            Progress = new ProgressSaveData { Day = 1, Phase = GamePhase.Day },
            Economy = new EconomySaveData(),
            Inventory = new InventorySaveData { Items = new List<InventoryItemSaveData>() },
            Order = new OrderSaveData { Id = "" },
            Story = new StorySaveData { Flags = new List<string>() },
            Unlocks = new UnlockSaveData { AbilityIds = new List<string>() },
            Underground = new UndergroundSaveData { Flags = new List<string>() },
            Package = new List<ItemStackSaveData>(),
            AcceptedOrders = new List<string>(),
            CompletedOrders = new List<string>()
        };
    }

    [Serializable] public sealed class ProgressSaveData { public int Day; public GamePhase Phase; }
    [Serializable] public sealed class EconomySaveData { public int Coins; }
    [Serializable] public sealed class InventorySaveData { public List<InventoryItemSaveData> Items; }
    [Serializable] public sealed class InventoryItemSaveData { public int Id; public int Quantity; }
    [Serializable] public sealed class OrderSaveData { public string Id; public float Progress; public bool Completed; }
    [Serializable] public sealed class StorySaveData { public List<string> Flags; }
    [Serializable] public sealed class UnlockSaveData { public List<string> AbilityIds; }
    [Serializable] public sealed class UndergroundSaveData { public List<string> Flags; }
}
