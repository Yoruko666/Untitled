using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Untitled.UI
{
    public sealed class UIQuickbar : MonoBehaviour
    {
        public UIItemSlot[] Slots;
        public UnityEvent<string> ActionRequested = new UnityEvent<string>();
        private InputActions input;
        private InputAction[] actions;
        private Action<InputAction.CallbackContext>[] callbacks;
        private void Awake()
        {
            input = new InputActions(); actions = new InputAction[5]; callbacks = new Action<InputAction.CallbackContext>[5];
            for (int i = 0; i < 5; i++)
            {
                int index = i; actions[i] = input.asset.FindAction("UIShortcuts/Slot" + (i + 1), true);
                callbacks[i] = context => Request(index); actions[i].performed += callbacks[i];
            }
        }
        private void OnEnable() { if (actions != null) foreach (var action in actions) action.Enable(); }
        private void OnDisable() { if (actions != null) foreach (var action in actions) action.Disable(); }
        private void OnDestroy()
        {
            if (input == null) return;
            for (int i = 0; i < 5; i++) actions[i].performed -= callbacks[i];
            input.Dispose(); input = null;
        }
        public void Bind(IReadOnlyList<UIItemData> items)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                Slots[i].Bind(items != null && i < items.Count ? items[i] : null);
                Slots[i].ActionRequested.RemoveListener(Forward); Slots[i].ActionRequested.AddListener(Forward);
            }
            gameObject.SetActive(items != null && items.Count > 0);
        }
        private void Forward(string id) { if (!UIManager.TryGet(out var ui) || !ui.HasOpenPanels) ActionRequested.Invoke(id); }
        private void Request(int index)
        {
            if (index >= Slots.Length || (UIManager.TryGet(out var ui) && ui.HasOpenPanels)) return;
            var item = Slots[index].Data;
            if (item != null && item.CanRequest) ActionRequested.Invoke(item.Id);
        }
    }
}
