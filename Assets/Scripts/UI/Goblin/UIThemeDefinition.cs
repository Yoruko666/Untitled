using TMPro;
using UnityEngine;

namespace Untitled.UI
{
    public enum UIButtonStyle { Primary, Secondary, Danger }
    public enum UIVisualRole { Body, Muted, Heading, Accent, Surface, Inset, Border, Progress, Danger }

    [CreateAssetMenu(menuName = "Untitled/UI/Theme", fileName = "UITheme")]
    public sealed class UIThemeDefinition : ScriptableObject
    {
        public string DisplayName;
        public Color Background, Surface, Inset, Border, Text, Muted, Heading, Accent, Danger;
        public Color PrimaryText, SecondaryText, DangerText;
        public TMP_FontAsset BodyFont, TitleFont;
        public Sprite BackgroundSprite, PanelSprite, BorderSprite, PrimaryButtonSprite, SecondaryButtonSprite, DangerButtonSprite;
        public Sprite MilkTeaIcon, QuestIcon, CoinIcon, PhaseIcon;
        public string IconStyle = "Pixel / Point / Placeholder";
        public float Spacing = 16f;
        public float Padding = 24f;
        public float SafeMargin = 64f;

        public Color ColorFor(UIVisualRole role)
        {
            switch (role)
            {
                case UIVisualRole.Muted: return Muted;
                case UIVisualRole.Heading: return Heading;
                case UIVisualRole.Accent: return Accent;
                case UIVisualRole.Surface: return Surface;
                case UIVisualRole.Inset: return Inset;
                case UIVisualRole.Border: return Border;
                case UIVisualRole.Progress: return Accent;
                case UIVisualRole.Danger: return Danger;
                default: return Text;
            }
        }
    }
}
