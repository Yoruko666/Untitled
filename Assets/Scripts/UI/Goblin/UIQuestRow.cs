using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIQuestRow : UIThemedView
    {
        [SerializeField] private TMP_Text title, detail, counter;
        [SerializeField] private Image icon;
        private UIQuestData data;
        private UIThemeDefinition theme;
        public void Configure(TMP_Text heading, TMP_Text body, TMP_Text count, Image image)
        { title = heading; detail = body; counter = count; icon = image; }
        public void Bind(UIQuestData value) { data = value; Render(); }
        private void Render()
        {
            if (data == null) return;
            title.text = data.Title ?? "";
            detail.text = data.Detail ?? "";
            counter.text = data.Counter ?? "";
            if (theme != null) counter.color = data.Complete ? theme.Accent : theme.Muted;
            icon.sprite = data.Icon != null ? data.Icon : theme?.QuestIcon;
            icon.enabled = icon.sprite != null;
        }
        public override void ApplyTheme(UIThemeDefinition value) { theme = value; Render(); }
    }
}
