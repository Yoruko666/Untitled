using System;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled.UI
{
    // Immutable display snapshots: rule evaluation stays with the producer.
    public sealed class UIOrderData
    {
        public string Id { get; }
        public string Title { get; }
        public string Customer { get; }
        public string Recipe { get; }
        public string Status { get; }
        public Sprite Icon { get; }
        public float Progress { get; }
        public bool CanRequest { get; }
        public UIOrderData(string id, string title, string customer, string recipe, string status,
            float progress, bool canRequest, Sprite icon = null)
        {
            Id = id; Title = title; Customer = customer; Recipe = recipe; Status = status;
            Progress = Mathf.Clamp01(progress); CanRequest = canRequest; Icon = icon;
        }
    }
    public sealed class UIQuestData
    {
        public string Title { get; }
        public string Detail { get; }
        public string Counter { get; }
        public bool Complete { get; }
        public Sprite Icon { get; }
        public UIQuestData(string title, string detail, string counter, bool complete, Sprite icon = null)
        { Title = title; Detail = detail; Counter = counter; Complete = complete; Icon = icon; }
    }
    public sealed class UISnapshot
    {
        public int Day { get; }
        public int Coins { get; }
        public UIOrderData Order { get; }
        public IReadOnlyList<UIQuestData> Quests { get; }
        public UISnapshot(int day, int coins, UIOrderData order, IReadOnlyList<UIQuestData> quests)
        {
            Day = day; Coins = coins; Order = order;
            Quests = quests == null ? Array.Empty<UIQuestData>() : new List<UIQuestData>(quests).AsReadOnly();
        }
    }
    public interface IUIReadModel
    {
        UISnapshot Current { get; }
        event Action<UISnapshot> Changed;
    }
}
