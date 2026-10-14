using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIDialogueBox : UIThemedView
    {
        [SerializeField] private UIPanel panel;
        [SerializeField] private TMP_Text line;
        [SerializeField] private Image portrait;
        [SerializeField] private RectTransform choicesRoot;
        [SerializeField] private UIButton choicePrefab, continueButton, closeButton;
        [SerializeField] private ScrollRect lineScroll, choicesScroll;
        public UnityEvent ContinueRequested = new UnityEvent();
        public UnityEvent CloseRequested = new UnityEvent();
        public UnityEvent<string> ChoiceRequested = new UnityEvent<string>();
        private readonly List<UIButton> choices = new List<UIButton>();
        private UIDialogueData data;
        private UIThemeDefinition theme;
        public UIDialogueData Data => data;
        public IReadOnlyList<UIButton> ChoiceButtons => choices.AsReadOnly();
        public UIButton ContinueButton => continueButton;
        public UIButton CloseButton => closeButton;
        public void Configure(UIPanel frame, TMP_Text body, Image image, RectTransform root, UIButton prefab,
            UIButton next, UIButton close, ScrollRect bodyScroll, ScrollRect optionScroll)
        { panel = frame; line = body; portrait = image; choicesRoot = root; choicePrefab = prefab; continueButton = next; closeButton = close; lineScroll = bodyScroll; choicesScroll = optionScroll; }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (continueButton != null) continueButton.Clicked.AddListener(Next);
            if (closeButton != null) closeButton.Clicked.AddListener(Close);
            Render();
        }
        protected override void OnDisable()
        {
            if (continueButton != null) continueButton.Clicked.RemoveListener(Next);
            if (closeButton != null) closeButton.Clicked.RemoveListener(Close);
            base.OnDisable();
        }
        public void Bind(UIDialogueData value) { data = value; Render(); if (lineScroll != null) lineScroll.verticalNormalizedPosition = 1; if (choicesScroll != null) choicesScroll.verticalNormalizedPosition = 1; }
        public void FocusDefault()
        {
            foreach (var choice in choices) if (choice.gameObject.activeInHierarchy && choice.Control.IsInteractable())
            { EventSystem.current?.SetSelectedGameObject(choice.gameObject); return; }
            EventSystem.current?.SetSelectedGameObject(continueButton.gameObject.activeInHierarchy && continueButton.Control.IsInteractable() ? continueButton.gameObject : closeButton.gameObject);
        }
        private void Next() { if (data != null && data.Choices.Count == 0) ContinueRequested.Invoke(); }
        private void Close() => CloseRequested.Invoke();
        private void RequestChoice(int index)
        { if (data != null && index < data.Choices.Count && data.Choices[index].Enabled) ChoiceRequested.Invoke(data.Choices[index].Id); }
        private void Render()
        {
            if (panel != null) panel.SetTitle(data?.Speaker ?? "NPC 对话");
            if (line != null) line.text = data?.Line ?? "今天想来一杯什么？";
            if (portrait != null) { portrait.sprite = data?.Portrait != null ? data.Portrait : theme?.NPCPortrait; portrait.enabled = portrait.sprite != null; }
            if (choicesRoot == null || choicePrefab == null) return;
            int count = data?.Choices.Count ?? 0;
            while (choices.Count < count)
            {
                int index = choices.Count;
                var button = Instantiate(choicePrefab, choicesRoot); button.Clicked.AddListener(() => RequestChoice(index)); choices.Add(button);
            }
            for (int i = 0; i < choices.Count; i++)
            {
                choices[i].SetVisible(i < count);
                if (i < count) { choices[i].SetLabel(data.Choices[i].Label); choices[i].SetInteractable(data.Choices[i].Enabled); }
            }
            if (continueButton != null) { continueButton.SetVisible(count == 0); continueButton.SetInteractable(data != null); }
            if (choicesScroll != null) choicesScroll.gameObject.SetActive(count > 0);
        }
        private void Update()
        {
            var keyboard = Keyboard.current; var selected = EventSystem.current?.currentSelectedGameObject;
            if (keyboard == null || selected == null || !selected.transform.IsChildOf(transform) || lineScroll == null) return;
            if (keyboard.pageUpKey.wasPressedThisFrame) lineScroll.verticalNormalizedPosition = Mathf.Clamp01(lineScroll.verticalNormalizedPosition + .25f);
            if (keyboard.pageDownKey.wasPressedThisFrame) lineScroll.verticalNormalizedPosition = Mathf.Clamp01(lineScroll.verticalNormalizedPosition - .25f);
        }
        public override void ApplyTheme(UIThemeDefinition value) { theme = value; Render(); }
    }
}
