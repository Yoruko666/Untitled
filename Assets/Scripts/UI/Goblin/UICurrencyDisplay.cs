using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UICurrencyDisplay : UIThemedView
    {
        [SerializeField] private TMP_Text value;
        [SerializeField] private Image icon;
        public void Configure(TMP_Text text, Image image) { value = text; icon = image; }
        public void Bind(int amount) { if (value != null) value.text = amount.ToString("N0"); }
        public void SetIcon(Sprite sprite) { if (icon != null) icon.sprite = sprite; }
        public override void ApplyTheme(UIThemeDefinition theme) { SetIcon(theme.CoinIcon); }
    }
}
