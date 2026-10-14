using System;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIItemData
    {
        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
        public string Description { get; }
        public int Quantity { get; }
        public Sprite Icon { get; }
        public bool CanRequest { get; }
        public UIItemData(string id, string name, string category, string description, int quantity, Sprite icon = null, bool canRequest = true)
        { Id = id; Name = name; Category = category; Description = description; Quantity = Mathf.Max(0, quantity); Icon = icon; CanRequest = canRequest; }
    }
    public sealed class UIInventorySnapshot
    {
        public string Title { get; }
        public IReadOnlyList<UIItemData> Items { get; }
        public UIInventorySnapshot(string title, IReadOnlyList<UIItemData> items)
        { Title = title; Items = items == null ? Array.Empty<UIItemData>() : new List<UIItemData>(items).AsReadOnly(); }
    }
    public interface IUIInventoryReadModel
    {
        UIInventorySnapshot Current { get; }
        event Action<UIInventorySnapshot> Changed;
    }
    public sealed class UIDialogueChoice
    {
        public string Id { get; }
        public string Label { get; }
        public bool Enabled { get; }
        public UIDialogueChoice(string id, string label, bool enabled = true) { Id = id; Label = label; Enabled = enabled; }
    }
    public sealed class UIDialogueData
    {
        public string Speaker { get; }
        public string Line { get; }
        public Sprite Portrait { get; }
        public IReadOnlyList<UIDialogueChoice> Choices { get; }
        public UIDialogueData(string speaker, string line, Sprite portrait = null, IReadOnlyList<UIDialogueChoice> choices = null)
        { Speaker = speaker; Line = line; Portrait = portrait; Choices = choices == null ? Array.Empty<UIDialogueChoice>() : new List<UIDialogueChoice>(choices).AsReadOnly(); }
    }
    public sealed class UIRewardData
    {
        public string Id { get; }
        public string Title { get; }
        public string Message { get; }
        public IReadOnlyList<UIItemData> Items { get; }
        public UIRewardData(string id, string title, string message, IReadOnlyList<UIItemData> items)
        { Id = id; Title = title; Message = message; Items = items == null ? Array.Empty<UIItemData>() : new List<UIItemData>(items).AsReadOnly(); }
    }
}
