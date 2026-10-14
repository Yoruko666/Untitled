using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Untitled.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UIButton : UIThemedView
    {
        [SerializeField] private UIButtonStyle style;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;
        [SerializeField] private bool panelSurface;
        public UnityEvent Clicked = new UnityEvent();
        public Button Control => GetComponent<Button>();
        public UIButtonStyle Style => style;

        public void Configure(UIButtonStyle value, TMP_Text text, Image image = null, bool usePanelSurface = false)
        {
            style = value;
            label = text;
            icon = image;
            panelSurface = usePanelSurface;
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            Control.onClick.AddListener(OnClick);
        }
        protected override void OnDisable()
        {
            Control.onClick.RemoveListener(OnClick);
            base.OnDisable();
        }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame) return;
            var system = EventSystem.current;
            if (system == null || system.currentSelectedGameObject != gameObject || !Control.IsInteractable()) return;
            // Respect hosts that already bind Space to Submit, avoiding a duplicate event.
            if (system.currentInputModule is InputSystemUIInputModule module && module.submit != null
                && module.submit.action != null && module.submit.action.WasPerformedThisFrame()) return;
            ExecuteEvents.Execute(gameObject, new BaseEventData(system), ExecuteEvents.submitHandler);
        }
        private void OnClick() => Clicked.Invoke();
        public void SetLabel(string value) { if (label != null) label.text = value ?? ""; }
        public void SetIcon(Sprite value)
        {
            if (icon == null) return;
            icon.sprite = value;
            icon.gameObject.SetActive(value != null);
            if (label != null) label.rectTransform.offsetMin = new Vector2(value != null ? 62 : 20, label.rectTransform.offsetMin.y);
        }
        public void SetInteractable(bool value) => Control.interactable = value;
        public override void ApplyTheme(UIThemeDefinition theme)
        {
            var image = Control.targetGraphic as Image;
            if (image != null)
            {
                image.sprite = panelSurface ? theme.PanelSprite : style == UIButtonStyle.Primary ? theme.PrimaryButtonSprite :
                    style == UIButtonStyle.Danger ? theme.DangerButtonSprite : theme.SecondaryButtonSprite;
                image.type = Image.Type.Sliced;
            }
            if (label != null)
            {
                label.font = theme.BodyFont;
                label.color = panelSurface ? theme.Text : style == UIButtonStyle.Primary ? theme.PrimaryText :
                    style == UIButtonStyle.Danger ? theme.DangerText : theme.SecondaryText;
            }
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.selectedColor = new Color(1.25f, 1.25f, 1.05f);
            colors.pressedColor = new Color(.72f, .72f, .72f);
            colors.disabledColor = new Color(.55f, .55f, .55f, .65f);
            colors.fadeDuration = .08f;
            Control.colors = colors;
        }
    }
}
