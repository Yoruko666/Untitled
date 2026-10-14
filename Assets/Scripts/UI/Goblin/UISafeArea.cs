using UnityEngine;

namespace Untitled.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class UISafeArea : UIThemedView
    {
        private RectTransform rect;
        private Canvas canvas;
        private float margin = 64;
        private Rect lastSafe;
        private Vector2Int lastSize;
        protected override void OnEnable()
        {
            rect = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            base.OnEnable();
            UpdateBounds();
        }
        public override void ApplyTheme(UIThemeDefinition theme) { margin = theme.SafeMargin; UpdateBounds(); }
        private void Update()
        {
            if (lastSafe != Screen.safeArea || lastSize.x != Screen.width || lastSize.y != Screen.height) UpdateBounds();
        }
        private void UpdateBounds()
        {
            if (rect == null || canvas == null || Screen.width <= 0 || Screen.height <= 0) return;
            lastSafe = Screen.safeArea; lastSize = new Vector2Int(Screen.width, Screen.height);
            rect.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
            rect.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height);
            rect.offsetMin = new Vector2(margin, margin); rect.offsetMax = new Vector2(-margin, -margin);
        }
    }
}
