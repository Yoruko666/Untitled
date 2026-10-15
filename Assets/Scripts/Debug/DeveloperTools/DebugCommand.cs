#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNTITLED_DEVELOPER_TOOLS)
using System;
using System.Collections.Generic;

namespace Untitled
{
    public enum DebugCommandKind
    {
        GetState, GetInventory, AddMoney, SetMoney, GiveItem, RemoveItem,
        ClearInventory, SetDay, SetDayMetadata, SetPhase
    }

    public enum DebugCommandStatus { Succeeded, Failed, Unavailable, ConfirmationRequired }

    public sealed class DebugCommandRequest
    {
        public DebugCommandKind Kind { get; }
        public int Value { get; }
        public int ItemId { get; }
        public GamePhase Phase { get; }
        public bool Confirmed { get; }

        public DebugCommandRequest(DebugCommandKind kind, int value = 0, int itemId = 0,
            GamePhase phase = GamePhase.Shop, bool confirmed = false)
        { Kind = kind; Value = value; ItemId = itemId; Phase = phase; Confirmed = confirmed; }

        public override string ToString() => $"{Kind} value={Value}, item={ItemId}, phase={Phase}, confirmed={Confirmed}";
    }

    public sealed class DebugInventoryItem
    {
        public int Id { get; }
        public int Count { get; }
        public DebugInventoryItem(int id, int count) { Id = id; Count = count; }
    }

    // Detached read model: UI never receives mutable gameplay or save DTO references.
    public sealed class DebugStateSnapshot
    {
        public int Day { get; }
        public GamePhase Phase { get; }
        public int Money { get; }
        public long UsedSlots { get; }
        public int Capacity { get; }
        public string Scene { get; }
        public bool NormalSaveBlocked { get; }
        public IReadOnlyList<DebugInventoryItem> Items { get; }
        public IReadOnlyList<string> AcceptedOrders { get; }
        public IReadOnlyList<string> StoryFlags { get; }

        internal DebugStateSnapshot(GameManager manager, string scene)
        {
            var game = manager.GameData;
            Day = game.Day; Phase = game.Phase; Money = game.Money; Scene = scene;
            NormalSaveBlocked = manager.DeveloperStateModified;
            UsedSlots = game.InventoryData.UsedSlots; Capacity = game.InventoryData.Capacity;
            var items = new List<DebugInventoryItem>();
            foreach (var item in game.InventoryData.Items) items.Add(new DebugInventoryItem(item.Id, item.Num));
            Items = items.AsReadOnly();
            AcceptedOrders = new List<string>(game.AcceptedOrders).AsReadOnly();
            StoryFlags = new List<string>(game.StoryFlags).AsReadOnly();
        }
    }

    public sealed class DebugCommandResult
    {
        public DateTime Time { get; } = DateTime.Now;
        public string Command { get; }
        public DebugCommandStatus Status { get; }
        public bool Success => Status == DebugCommandStatus.Succeeded;
        public string Message { get; }
        public DebugStateSnapshot State { get; }

        internal DebugCommandResult(string command, DebugCommandStatus status, string message, DebugStateSnapshot state)
        { Command = command; Status = status; Message = message; State = state; }
    }
}
#endif
