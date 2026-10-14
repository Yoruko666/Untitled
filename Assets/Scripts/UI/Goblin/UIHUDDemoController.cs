using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIHUDDemoController : MonoBehaviour
    {
        public UIHUDMockSource Mock;
        public Image Background;
        public UIThemeContext Theme;
        private void OnEnable() { Mock.Changed += OnData; OnData(Mock.Current); }
        private void OnDisable() => Mock.Changed -= OnData;
        private void OnData(UIHUDData data) => Background.color = Theme.Current.Background;
        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.nKey.wasPressedThisFrame) { Mock.TogglePhase(); OnData(Mock.Current); }
            if (Keyboard.current.lKey.wasPressedThisFrame && (!UIManager.TryGet(out var ui) || !ui.HasOpenPanels)) Mock.ToggleAlert();
        }
    }
}
