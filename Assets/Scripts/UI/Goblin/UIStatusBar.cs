using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIStatusBar : UIThemedView
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon, fill;
        [SerializeField] private bool danger;
        public float Fraction => fill != null ? fill.fillAmount : 0;
        public void Configure(TMP_Text text, Image image, Image bar, bool health) { label = text; icon = image; fill = bar; danger = health; }
        public void Bind(UIStatusData data)
        {
            SetVisible(data != null);
            if (data == null) return;
            label.text = data.Label + "  " + data.Value.ToString("0") + "/" + data.Maximum.ToString("0");
            fill.preserveAspect = false;
            fill.fillAmount = data.Maximum > 0 ? Mathf.Clamp01(data.Value / data.Maximum) : 0;
            icon.sprite = data.Icon; icon.enabled = data.Icon != null;
        }
        public override void ApplyTheme(UIThemeDefinition theme) { if (fill != null) fill.color = danger ? theme.Danger : theme.Accent; }
    }
}
