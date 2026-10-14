using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIExtendedShowcaseController : MonoBehaviour
    {
        public UIThemeContext Theme;
        public UIExtendedMockSource Mock;
        public UIInventoryPanel Inventory;
        public UIItemTooltip Tooltip;
        public UIDialogueBox Dialogue;
        public UIRewardPopup Reward;
        public UIButton ThemeButton, RefreshButton, InventoryButton, DialogueButton, RewardButton;
        public TMP_Text Feedback;
        public Image Backdrop, PhaseIcon;
        private void OnEnable()
        {
            if (Theme == null) return;
            Theme.Changed += OnTheme;
            ThemeButton.Clicked.AddListener(ToggleTheme); RefreshButton.Clicked.AddListener(Refresh);
            InventoryButton.Clicked.AddListener(ToggleInventory); DialogueButton.Clicked.AddListener(OpenDialogue); RewardButton.Clicked.AddListener(OpenReward);
            Inventory.PreviewChanged += PreviewItem; Inventory.ItemRequested.AddListener(RequestItem);
            Dialogue.ContinueRequested.AddListener(OnContinue); Dialogue.CloseRequested.AddListener(CloseDialogue); Dialogue.ChoiceRequested.AddListener(OnChoice);
            Reward.Acknowledged.AddListener(OnRewardRead); Reward.CloseRequested.AddListener(OnRewardClose);
            OnTheme(Theme.Current);
        }
        private void OnDisable()
        {
            if (Theme == null) return;
            Theme.Changed -= OnTheme;
            ThemeButton.Clicked.RemoveListener(ToggleTheme); RefreshButton.Clicked.RemoveListener(Refresh);
            InventoryButton.Clicked.RemoveListener(ToggleInventory); DialogueButton.Clicked.RemoveListener(OpenDialogue); RewardButton.Clicked.RemoveListener(OpenReward);
            Inventory.PreviewChanged -= PreviewItem; Inventory.ItemRequested.RemoveListener(RequestItem);
            Dialogue.ContinueRequested.RemoveListener(OnContinue); Dialogue.CloseRequested.RemoveListener(CloseDialogue); Dialogue.ChoiceRequested.RemoveListener(OnChoice);
            Reward.Acknowledged.RemoveListener(OnRewardRead); Reward.CloseRequested.RemoveListener(OnRewardClose);
        }
        private void Start()
        {
            Dialogue.Bind(Mock.Greeting);
            Tooltip.Bind(Mock.Current.Items[4]);
            EventSystem.current?.SetSelectedGameObject(ThemeButton.gameObject);
        }
        public void ToggleTheme() => Theme.Toggle();
        public void Refresh() { Mock.NextSample(); Feedback.text = "背包快照已更新 · 大于 12 格时可滚动查看"; }
        public void ToggleInventory()
        {
            Inventory.SetVisible(!Inventory.gameObject.activeSelf);
            InventoryButton.SetLabel(Inventory.gameObject.activeSelf ? "收起背包" : "打开背包");
            if (!Inventory.gameObject.activeSelf) { Tooltip.Hide(); EventSystem.current?.SetSelectedGameObject(InventoryButton.gameObject); }
        }
        private void PreviewItem(UIItemData value) { if (value == null) Tooltip.Bind(null); else Tooltip.Show(value); }
        private void RequestItem(string id) => Feedback.text = "物品操作请求：" + id + " · 数量由物品系统决定";
        public void OpenDialogue()
        { Dialogue.Bind(Mock.Greeting); Dialogue.SetVisible(true); EventSystem.current?.SetSelectedGameObject(Dialogue.CloseButton.gameObject); }
        private void CloseDialogue() { Dialogue.SetVisible(false); EventSystem.current?.SetSelectedGameObject(DialogueButton.gameObject); }
        private void OnChoice(string id) { Dialogue.Bind(Mock.Reply); Feedback.text = "对话选项已发送：" + id; EventSystem.current?.SetSelectedGameObject(Dialogue.ContinueButton.gameObject); }
        private void OnContinue() { CloseDialogue(); Feedback.text = "对话演示结束 · 正式节点推进由现有对话系统处理"; }
        public void OpenReward() => Reward.Open(Mock.Reward);
        private void OnRewardRead(string id) => Feedback.text = "奖励已阅：" + id + " · 没有修改金币或物品";
        private void OnRewardClose() => Feedback.text = "奖励弹窗已关闭 · 焦点恢复到原按钮";
        private void OnTheme(UIThemeDefinition theme)
        {
            if (theme == null) return;
            ThemeButton.SetLabel(Theme.IsNight ? "切换到白天" : "切换到夜晚");
            Backdrop.color = theme.Background; PhaseIcon.sprite = theme.PhaseIcon;
        }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.tKey.wasPressedThisFrame) ToggleTheme();
            if (Reward.gameObject.activeSelf) return;
            if (keyboard.bKey.wasPressedThisFrame) ToggleInventory();
            if (keyboard.rKey.wasPressedThisFrame) Refresh();
            if (keyboard.tabKey.wasPressedThisFrame)
            {
                var system = EventSystem.current; if (system == null) return;
                var controls = GetComponentsInChildren<Selectable>(false);
                int current = -1;
                for (int i = 0; i < controls.Length; i++) if (controls[i].gameObject == system.currentSelectedGameObject) current = i;
                int direction = keyboard.shiftKey.isPressed ? -1 : 1;
                for (int i = 0; i < controls.Length; i++)
                { current = (current + direction + controls.Length) % controls.Length; if (controls[current].IsInteractable()) { system.SetSelectedGameObject(controls[current].gameObject); break; } }
            }
        }
    }
}
