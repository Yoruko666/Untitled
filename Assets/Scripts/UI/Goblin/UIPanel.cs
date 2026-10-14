using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Untitled.UI
{
    // Visual container. Existing Untitled.PanelBase/UIManager continue to own modal business lifecycle.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIPanel : UIThemedView
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text title;
        [SerializeField] private Image titleIcon;
        [SerializeField] private RectTransform content;
        public RectTransform Content => content;
        public void Configure(Image image, TMP_Text heading, Image icon, RectTransform body)
        {
            background = image; title = heading; titleIcon = icon; content = body;
        }
        public void SetTitle(string value) { if (title != null) title.text = value ?? ""; }
        public void SetIcon(Sprite value)
        {
            if (titleIcon == null) return;
            titleIcon.sprite = value;
            titleIcon.gameObject.SetActive(value != null);
        }
        public void Show() => SetVisible(true);
        public void Hide() => SetVisible(false);
        public override void ApplyTheme(UIThemeDefinition theme)
        {
            if (background != null) { background.sprite = theme.PanelSprite; background.type = Image.Type.Sliced; }
            if (title != null) { title.font = theme.TitleFont; title.color = theme.Heading; }
            if (content != null && content.TryGetComponent<LayoutGroup>(out var layout))
            {
                int padding = Mathf.RoundToInt(theme.Padding);
                layout.padding = new RectOffset(padding, padding, padding, padding);
                if (layout is VerticalLayoutGroup vertical) vertical.spacing = theme.Spacing;
                if (layout is HorizontalLayoutGroup horizontal) horizontal.spacing = theme.Spacing;
            }
        }
    }
}
