using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIRewardPopup : UIThemedView
    {
        [SerializeField] private UIPanel panel;
        [SerializeField] private TMP_Text message;
        [SerializeField] private RectTransform itemsRoot;
        [SerializeField] private UIItemSlot slotPrefab;
        [SerializeField] private UIButton confirmButton, closeButton;
        [SerializeField] private ScrollRect rewardScroll;
        public UnityEvent<string> Acknowledged = new UnityEvent<string>();
        public UnityEvent CloseRequested = new UnityEvent();
        private readonly List<UIItemSlot> slots = new List<UIItemSlot>();
        private UIRewardData data;
        private bool acknowledged;
        private GameObject previousFocus;
        public UIRewardData Data => data;
        public UIButton ConfirmButton => confirmButton;
        public void Configure(UIPanel frame, TMP_Text subtitle, RectTransform root, UIItemSlot prefab, UIButton confirm, UIButton close, ScrollRect scroll = null)
        { panel = frame; message = subtitle; itemsRoot = root; slotPrefab = prefab; confirmButton = confirm; closeButton = close; rewardScroll = scroll; }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (confirmButton == null) return;
            previousFocus = EventSystem.current?.currentSelectedGameObject;
            confirmButton.Clicked.AddListener(Acknowledge);
            closeButton.Clicked.AddListener(Close);
            EventSystem.current?.SetSelectedGameObject(data == null ? closeButton.gameObject : confirmButton.gameObject);
        }
        protected override void OnDisable()
        {
            if (confirmButton != null) confirmButton.Clicked.RemoveListener(Acknowledge);
            if (closeButton != null) closeButton.Clicked.RemoveListener(Close);
            if (previousFocus != null && previousFocus.activeInHierarchy) EventSystem.current?.SetSelectedGameObject(previousFocus);
            previousFocus = null;
            base.OnDisable();
        }
        public void Open(UIRewardData value)
        {
            data = value; acknowledged = false;
            Render(); gameObject.SetActive(true);
            if (rewardScroll != null) rewardScroll.verticalNormalizedPosition = 1;
            EventSystem.current?.SetSelectedGameObject(data == null ? closeButton.gameObject : confirmButton.gameObject);
        }
        private void Render()
        {
            if (panel == null || itemsRoot == null || slotPrefab == null) return;
            panel.SetTitle(data?.Title ?? "获得奖励");
            message.text = data?.Message ?? "";
            int count = data?.Items.Count ?? 0;
            while (slots.Count < count) slots.Add(Instantiate(slotPrefab, itemsRoot));
            for (int i = 0; i < slots.Count; i++) { slots[i].SetVisible(i < count); if (i < count) slots[i].Bind(data.Items[i], true); }
            confirmButton.SetInteractable(data != null && !acknowledged);
        }
        private void Acknowledge()
        {
            if (data == null || acknowledged) return;
            acknowledged = true; confirmButton.SetInteractable(false);
            Acknowledged.Invoke(data.Id); SetVisible(false);
        }
        public void Close() { CloseRequested.Invoke(); SetVisible(false); }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (rewardScroll != null && keyboard.pageUpKey.wasPressedThisFrame) rewardScroll.verticalNormalizedPosition = Mathf.Clamp01(rewardScroll.verticalNormalizedPosition + .25f);
            if (rewardScroll != null && keyboard.pageDownKey.wasPressedThisFrame) rewardScroll.verticalNormalizedPosition = Mathf.Clamp01(rewardScroll.verticalNormalizedPosition - .25f);
            if (keyboard.tabKey.wasPressedThisFrame)
            {
                var current = EventSystem.current?.currentSelectedGameObject;
                EventSystem.current?.SetSelectedGameObject(current == confirmButton.gameObject ? closeButton.gameObject : confirmButton.gameObject);
            }
        }
        public override void ApplyTheme(UIThemeDefinition theme) { }
    }
}
