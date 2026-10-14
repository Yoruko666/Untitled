using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIModalFocus : MonoBehaviour
    {
        public Selectable First;
        private Selectable[] controls;
        private GameObject previous;
        private void OnEnable()
        {
            previous = EventSystem.current?.currentSelectedGameObject;
            controls = GetComponentsInChildren<Selectable>(false);
            EventSystem.current?.SetSelectedGameObject(First != null ? First.gameObject : null);
        }
        private void OnDisable()
        {
            EventSystem.current?.SetSelectedGameObject(previous != null && previous.activeInHierarchy ? previous : null);
            previous = null;
        }
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame || EventSystem.current == null || controls.Length == 0) return;
            int current = -1, direction = Keyboard.current.shiftKey.isPressed ? -1 : 1;
            for (int i = 0; i < controls.Length; i++) if (controls[i].gameObject == EventSystem.current.currentSelectedGameObject) current = i;
            for (int i = 0; i < controls.Length; i++)
            {
                current = (current + direction + controls.Length) % controls.Length;
                if (controls[current].IsInteractable() && controls[current].gameObject.activeInHierarchy) { EventSystem.current.SetSelectedGameObject(controls[current].gameObject); return; }
            }
        }
    }
}
