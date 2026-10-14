using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Untitled.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIItemTooltip : UIThemedView
    {
        [SerializeField] private UIPanel panel;
        [SerializeField] private TMP_Text category, description;
        [SerializeField] private ScrollRect descriptionScroll;
        private UIItemData data;
        private UIThemeDefinition theme;
        public UIItemData Data => data;
        public void Configure(UIPanel frame, TMP_Text categoryText, TMP_Text body, ScrollRect scroll)
        { panel = frame; category = categoryText; description = body; descriptionScroll = scroll; }
        public void Bind(UIItemData value) { data = value; Render(); if (descriptionScroll != null) descriptionScroll.verticalNormalizedPosition = 1; }
        public void Show(UIItemData value)
        { Bind(value); gameObject.SetActive(true); GetComponent<CanvasGroup>().blocksRaycasts = true; }
        public void Hide() => SetVisible(false);
        public void ScrollDescription(float delta)
        { if (descriptionScroll != null) descriptionScroll.verticalNormalizedPosition = Mathf.Clamp01(descriptionScroll.verticalNormalizedPosition + delta); }
        public void ShowAt(UIItemData value, Vector2 screenPoint)
        {
            Show(value);
            GetComponent<CanvasGroup>().blocksRaycasts = false;
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (parent == null) return;
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, camera, out var point)) return;
            Canvas.ForceUpdateCanvases();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            var bounds = parent.rect; const float inset = 16;
            float x = Mathf.Clamp(point.x + inset, bounds.xMin + inset, Mathf.Max(bounds.xMin + inset, bounds.xMax - rect.rect.width - inset));
            float y = Mathf.Clamp(point.y - inset, Mathf.Min(bounds.yMax - inset, bounds.yMin + rect.rect.height + inset), bounds.yMax - inset);
            rect.anchoredPosition = new Vector2(x - bounds.xMin, y - bounds.yMax);
        }
        private void Render()
        {
            if (panel != null) { panel.SetTitle(data?.Name ?? "道具说明"); panel.SetIcon(data?.Icon != null ? data.Icon : theme?.ItemFallbackIcon); }
            if (category != null) category.text = data == null ? "选择物品查看详情" : data.Category + "  /  数量 " + data.Quantity;
            if (description != null) description.text = data?.Description ?? "物品不会自己长腿，除非它叫哥布林。";
        }
        public override void ApplyTheme(UIThemeDefinition value) { theme = value; Render(); }
    }
}
