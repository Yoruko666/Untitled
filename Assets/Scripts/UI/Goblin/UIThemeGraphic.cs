using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIThemeGraphic : UIThemedView
    {
        public UIVisualRole Role = UIVisualRole.Body;
        public bool UsePanelSprite;
        public bool UseBackgroundSprite;
        public override void ApplyTheme(UIThemeDefinition theme)
        {
            if (TryGetComponent<TMP_Text>(out var text))
            {
                text.color = theme.ColorFor(Role);
                text.font = Role == UIVisualRole.Heading ? theme.TitleFont : theme.BodyFont;
            }
            else if (TryGetComponent<Image>(out var image))
            {
                image.color = UsePanelSprite || UseBackgroundSprite ? Color.white : theme.ColorFor(Role);
                if (UsePanelSprite) image.sprite = theme.PanelSprite;
                if (UseBackgroundSprite) image.sprite = theme.BackgroundSprite;
            }
        }
    }
}
