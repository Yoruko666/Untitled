using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIShowcaseController : MonoBehaviour
    {
        public UIThemeContext Theme;
        public UIMockDataSource Mock;
        public UIPanel OrderPanel;
        public UIButton ThemeButton, DataButton, VisibilityButton, SecondaryButton, DangerButton;
        public TMP_Text Feedback;
        public Image Backdrop, PhaseIcon;
        public ScrollRect QuestScroll;
        private void OnEnable()
        {
            if (Theme == null) return;
            Theme.Changed += OnTheme;
            ThemeButton.Clicked.AddListener(ToggleTheme);
            DataButton.Clicked.AddListener(NextData);
            VisibilityButton.Clicked.AddListener(ToggleOrder);
            SecondaryButton.Clicked.AddListener(SecondaryFeedback);
            DangerButton.Clicked.AddListener(DangerFeedback);
            OnTheme(Theme.Current);
        }
        private void OnDisable()
        {
            if (Theme == null) return;
            Theme.Changed -= OnTheme;
            ThemeButton.Clicked.RemoveListener(ToggleTheme);
            DataButton.Clicked.RemoveListener(NextData);
            VisibilityButton.Clicked.RemoveListener(ToggleOrder);
            SecondaryButton.Clicked.RemoveListener(SecondaryFeedback);
            DangerButton.Clicked.RemoveListener(DangerFeedback);
        }
        private void Start() => EventSystem.current?.SetSelectedGameObject(ThemeButton.gameObject);
        public void ToggleTheme() => Theme.Toggle();
        public void NextData() { Mock.NextSample(); Feedback.text = "演示数据已更新 · 订单、金币与任务同步刷新"; }
        public void ToggleOrder()
        {
            OrderPanel.SetVisible(!OrderPanel.gameObject.activeSelf);
            VisibilityButton.SetLabel(OrderPanel.gameObject.activeSelf ? "收起订单" : "打开订单");
            Feedback.text = OrderPanel.gameObject.activeSelf ? "订单已重新打开 · 原数据保持" : "订单已收起 · 按钮仍可重新打开";
        }
        private void SecondaryFeedback() => Feedback.text = "次按钮已响应 · 输入由 UI 转发给调用方";
        private void DangerFeedback() => Feedback.text = "危险按钮已响应 · 演示中没有执行游戏操作";
        public void OnOrderRequested(string id) => Feedback.text = "制作台请求已发出 · 订单 " + id;
        private void OnTheme(UIThemeDefinition value)
        {
            if (value == null) return;
            ThemeButton.SetLabel(Theme.IsNight ? "切换到白天" : "切换到夜晚");
            Backdrop.color = value.Background;
            PhaseIcon.sprite = value.PhaseIcon;
        }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (QuestScroll != null && keyboard.pageUpKey.wasPressedThisFrame) QuestScroll.verticalNormalizedPosition = Mathf.Clamp01(QuestScroll.verticalNormalizedPosition + .25f);
            if (QuestScroll != null && keyboard.pageDownKey.wasPressedThisFrame) QuestScroll.verticalNormalizedPosition = Mathf.Clamp01(QuestScroll.verticalNormalizedPosition - .25f);
            if (keyboard.tKey.wasPressedThisFrame) ToggleTheme();
            if (keyboard.rKey.wasPressedThisFrame) NextData();
            if (keyboard.oKey.wasPressedThisFrame) ToggleOrder();
            if (keyboard.tabKey.wasPressedThisFrame) MoveFocus(keyboard.shiftKey.isPressed ? -1 : 1);
        }
        private void MoveFocus(int direction)
        {
            var system = EventSystem.current;
            if (system == null) return;
            var controls = GetComponentsInChildren<Selectable>(false);
            int index = -1;
            for (int i = 0; i < controls.Length; i++) if (controls[i].gameObject == system.currentSelectedGameObject) index = i;
            for (int i = 0; i < controls.Length; i++)
            {
                index = (index + direction + controls.Length) % controls.Length;
                if (controls[index].IsInteractable()) { system.SetSelectedGameObject(controls[index].gameObject); return; }
            }
        }
    }
}
