using TMPro;
using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIInteractionPrompt : UIThemedView
    {
        [SerializeField] private TMP_Text key, label;
        public void Configure(TMP_Text keyText, TMP_Text text) { key = keyText; label = text; }
        public void Bind(string description, string binding = "E")
        {
            if (key != null) key.text = binding;
            if (label != null) label.text = description ?? "";
            SetVisible(!string.IsNullOrWhiteSpace(description));
        }
        public override void ApplyTheme(UIThemeDefinition theme) { }
    }
}
