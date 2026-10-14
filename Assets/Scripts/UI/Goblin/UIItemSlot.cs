using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Untitled.UI
{
    [RequireComponent(typeof(UIButton))]
    public sealed class UIItemSlot : UIThemedView, IPointerEnterHandler, ISelectHandler
    {
        [SerializeField] private UIButton button;
        [SerializeField] private TMP_Text itemName, quantity;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selectedMark;
        public event Action<UIItemData> Previewed;
        public UnityEvent<string> ActionRequested = new UnityEvent<string>();
        private UIItemData data;
        private UIThemeDefinition theme;
        private bool previewOnly;
        public UIItemData Data => data;
        public UIButton Button => button;
        public void Configure(UIButton control, TMP_Text nameText, TMP_Text countText, Image image, GameObject mark)
        { button = control; itemName = nameText; quantity = countText; icon = image; selectedMark = mark; }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (button != null) button.Clicked.AddListener(Request);
            Render();
        }
        protected override void OnDisable()
        {
            if (button != null) button.Clicked.RemoveListener(Request);
            base.OnDisable();
        }
        public void Bind(UIItemData value, bool displayOnly = false)
        { data = value; previewOnly = displayOnly; SetSelected(false); Render(); }
        public void SetSelected(bool selected) { if (selectedMark != null) selectedMark.SetActive(selected); }
        public void OnPointerEnter(PointerEventData eventData) => Preview();
        public void OnSelect(BaseEventData eventData) => Preview();
        private void Preview() { if (data != null && !previewOnly) Previewed?.Invoke(data); }
        private void Request()
        {
            Preview();
            if (data != null && !previewOnly && data.CanRequest) ActionRequested.Invoke(data.Id);
        }
        private void Render()
        {
            if (itemName != null) itemName.text = data?.Name ?? "空格";
            if (quantity != null) quantity.text = data == null ? "" : data.Quantity.ToString();
            if (icon != null) { icon.sprite = data?.Icon != null ? data.Icon : theme?.ItemFallbackIcon; icon.enabled = data != null && icon.sprite != null; }
            if (button != null)
            {
                button.SetInteractable(data != null && !previewOnly);
                var colors = button.Control.colors;
                colors.disabledColor = previewOnly ? Color.white : new Color(.65f, .65f, .65f, .8f);
                button.Control.colors = colors;
            }
        }
        public override void ApplyTheme(UIThemeDefinition value) { theme = value; Render(); }
    }
}
