using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Untitled.UI
{
    public sealed class UIPauseMenu : UIThemedView
    {
        public UIButton ResumeButton, HelpButton;
        public TMP_Text Help;
        public UnityEvent ResumeRequested = new UnityEvent();
        protected override void OnEnable()
        {
            base.OnEnable();
            if (ResumeButton == null) return;
            ResumeButton.Clicked.AddListener(Resume); HelpButton.Clicked.AddListener(ToggleHelp);
            Help.gameObject.SetActive(false);
        }
        protected override void OnDisable()
        {
            if (ResumeButton != null) ResumeButton.Clicked.RemoveListener(Resume);
            if (HelpButton != null) HelpButton.Clicked.RemoveListener(ToggleHelp);
            base.OnDisable();
        }
        private void Resume() => ResumeRequested.Invoke();
        private void ToggleHelp() => Help.gameObject.SetActive(!Help.gameObject.activeSelf);
        public override void ApplyTheme(UIThemeDefinition theme) { }
    }
}
