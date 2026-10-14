using System;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIStatusData
    {
        public string Label { get; }
        public float Value { get; }
        public float Maximum { get; }
        public Sprite Icon { get; }
        public UIStatusData(string label, float value, float maximum, Sprite icon = null) { Label = label; Value = value; Maximum = maximum; Icon = icon; }
    }
    public sealed class UIHUDData
    {
        public int Day { get; }
        public int Coins { get; }
        public bool Night { get; }
        public UIOrderData Order { get; }
        public IReadOnlyList<UIQuestData> Quests { get; }
        public UIStatusData Health { get; }
        public UIStatusData Resource { get; }
        public IReadOnlyList<UIItemData> Quickbar { get; }
        public string Prompt { get; }
        public bool Alert { get; }
        public UIHUDData(int day, int coins, bool night, UIOrderData order, IReadOnlyList<UIQuestData> quests,
            UIStatusData health = null, UIStatusData resource = null, IReadOnlyList<UIItemData> quickbar = null, string prompt = null, bool alert = false)
        {
            Day = day; Coins = coins; Night = night; Order = order;
            Quests = quests == null ? Array.Empty<UIQuestData>() : new List<UIQuestData>(quests).AsReadOnly();
            Health = health; Resource = resource;
            Quickbar = quickbar == null ? Array.Empty<UIItemData>() : new List<UIItemData>(quickbar).AsReadOnly();
            Prompt = prompt; Alert = alert;
        }
    }
    public interface IUIHUDReadModel
    {
        UIHUDData Current { get; }
        event Action<UIHUDData> Changed;
    }
}
