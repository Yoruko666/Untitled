using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIInventoryPanel : UIThemedView
    {
        [SerializeField] private UIPanel panel;
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private UIItemSlot slotPrefab;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TMP_Text status;
        [SerializeField] private UIButton allButton, materialButton, questButton;
        [SerializeField] private int minimumSlots = 12;
        public event Action<UIItemData> PreviewChanged;
        public UnityEvent<string> ItemRequested = new UnityEvent<string>();
        private readonly List<UIItemSlot> slots = new List<UIItemSlot>();
        private readonly List<UIItemData> visible = new List<UIItemData>();
        private UIInventorySnapshot snapshot;
        private string filter = "", selectedId;
        public UIInventorySnapshot Data => snapshot;
        public IReadOnlyList<UIItemSlot> Slots => slots.AsReadOnly();
        public string Filter => filter;
        public void Configure(UIPanel frame, RectTransform grid, UIItemSlot prefab, ScrollRect view, TMP_Text statusText,
            UIButton all, UIButton materials, UIButton quests)
        { panel = frame; gridRoot = grid; slotPrefab = prefab; scroll = view; status = statusText; allButton = all; materialButton = materials; questButton = quests; }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (allButton != null) allButton.Clicked.AddListener(ShowAll);
            if (materialButton != null) materialButton.Clicked.AddListener(ShowMaterials);
            if (questButton != null) questButton.Clicked.AddListener(ShowQuests);
            Render();
        }
        protected override void OnDisable()
        {
            if (allButton != null) allButton.Clicked.RemoveListener(ShowAll);
            if (materialButton != null) materialButton.Clicked.RemoveListener(ShowMaterials);
            if (questButton != null) questButton.Clicked.RemoveListener(ShowQuests);
            base.OnDisable();
        }
        public void Bind(UIInventorySnapshot value) { snapshot = value; Render(); }
        public void ShowAll() => SetFilter("");
        public void ShowMaterials() => SetFilter("材料");
        public void ShowQuests() => SetFilter("任务");
        public void SetFilter(string value) { filter = value ?? ""; Render(); if (scroll != null) scroll.verticalNormalizedPosition = 1; }
        private void Render()
        {
            if (gridRoot == null || slotPrefab == null) return;
            panel.SetTitle(snapshot?.Title ?? "背包");
            visible.Clear();
            if (snapshot != null) foreach (var item in snapshot.Items) if (item != null && (filter.Length == 0 || item.Category == filter)) visible.Add(item);
            int count = Mathf.Max(minimumSlots, visible.Count);
            while (slots.Count < count)
            {
                var slot = Instantiate(slotPrefab, gridRoot);
                slot.Previewed += OnPreview;
                slot.ActionRequested.AddListener(OnRequest);
                slots.Add(slot);
            }
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].SetVisible(i < count);
                if (i < count) { slots[i].Bind(i < visible.Count ? visible[i] : null); slots[i].SetSelected(slots[i].Data?.Id == selectedId && selectedId != null); }
            }
            UIItemData selected = visible.Find(item => item.Id == selectedId);
            if (selectedId != null && selected == null) selectedId = null;
            PreviewChanged?.Invoke(selected);
            if (status != null) status.text = visible.Count + " 种物品  /  " + (filter.Length == 0 ? "全部" : filter) + "  ·  点击或 Enter 查看";
        }
        private void OnPreview(UIItemData value)
        {
            selectedId = value.Id;
            foreach (var slot in slots) slot.SetSelected(slot.Data?.Id == selectedId);
            PreviewChanged?.Invoke(value);
        }
        private void OnRequest(string id) => ItemRequested.Invoke(id);
        private void Update()
        {
            var system = EventSystem.current; var keyboard = Keyboard.current;
            if (scroll == null || keyboard == null || system == null || system.currentSelectedGameObject == null
                || !system.currentSelectedGameObject.transform.IsChildOf(transform)) return;
            if (keyboard.pageUpKey.wasPressedThisFrame) scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + .25f);
            if (keyboard.pageDownKey.wasPressedThisFrame) scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition - .25f);
        }
        public override void ApplyTheme(UIThemeDefinition value) { }
    }
}
