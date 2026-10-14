using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIGameplayHUD : MonoBehaviour
    {
        public MonoBehaviour Source;
        public UIThemeContext Theme;
        public UnityEngine.UI.Image PhaseIcon;
        public TMP_Text Day, Order;
        public GameObject OrderRoot, Crosshair, Alert;
        public UICurrencyDisplay Coins;
        public UIStatusBar Health, Resource;
        public UIQuestTracker Quests;
        public UIQuickbar Quickbar;
        public UIInteractionPrompt Prompt;
        public UIButton PauseButton;
        private IUIHUDReadModel model;
        private UIManager manager;
        private UIHUDData current;
        private void OnEnable()
        {
            model = Source as IUIHUDReadModel;
            if (model != null) { model.Changed += Render; Render(model.Current); }
            if (PauseButton != null) PauseButton.Clicked.AddListener(OpenPause);
            if (manager != null) { manager.ModalStateChanged += OnModal; OnModal(manager.HasOpenPanels); }
        }
        private void Start()
        { if (UIManager.TryGet(out manager)) { manager.ModalStateChanged += OnModal; OnModal(manager.HasOpenPanels); } }
        private void OnDisable()
        {
            if (model != null) model.Changed -= Render;
            if (manager != null) manager.ModalStateChanged -= OnModal;
            if (PauseButton != null) PauseButton.Clicked.RemoveListener(OpenPause);
        }
        public void OpenPause() { if (UIManager.TryGet(out var ui)) ui.Open(Untitled.UIPanel.Menu); }
        private void OnModal(bool open) { if (current != null) { Prompt.Bind(open ? null : current.Prompt); Crosshair.SetActive(!current.Night && !open); } }
        private void Render(UIHUDData data)
        {
            if (data == null) return;
            current = data; Theme.Apply(data.Night);
            if (PhaseIcon != null) PhaseIcon.sprite = Theme.Current.PhaseIcon;
            Day.text = (data.Night ? "夜晚 " : "白天 ") + "第 " + data.Day + " 天"; Coins.Bind(data.Coins);
            OrderRoot.SetActive(!data.Night); Order.text = data.Order?.Title ?? "等待顾客下单";
            Health.Bind(data.Night ? data.Health : null); Resource.Bind(data.Night ? data.Resource : null);
            var brief = new List<UIQuestData>();
            foreach (var quest in data.Quests) brief.Add(new UIQuestData(quest.Title, "", quest.Counter, quest.Complete, quest.Icon));
            Quests.Bind(brief); Quests.SetVisible(data.Night && brief.Count > 0);
            Quickbar.Bind(data.Night ? data.Quickbar : null);
            Alert.SetActive(data.Night && data.Alert);
            bool modal = manager != null && manager.HasOpenPanels;
            Prompt.Bind(modal ? null : data.Prompt); Crosshair.SetActive(!data.Night && !modal);
        }
    }
}
