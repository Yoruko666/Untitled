using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Untitled.Config;

namespace Untitled
{
    public class DialoguePanel : PanelBase
    {
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform optionRoot;
        [SerializeField] private Button optionButtonTemplate;

        private readonly List<Button> optionButtons = new();
        private Dialog currentNode;

        protected override void OnInit()
        {
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (closeButton != null) closeButton.onClick.AddListener(OnLeave);
        }

        protected override void OnShow(object data)
        {
            if (data is not DialogRequest request)
            {
                Debug.LogError("[DialoguePanel] 没有拿到对话请求。");
                UIManager.Instance.Close(this);
                return;
            }

            ShowNode(request.StartId);
        }

        protected override void OnHide()
        {
            ClearOptions();
            currentNode = null;
        }

        private void OnNext()
        {
            Advance(currentNode == null ? 0 : currentNode.NextId);
        }

        private void OnLeave()
        {
            UIManager.Instance.Close(this);
        }

        private void ShowNode(int id)
        {
            Dialog node = DatabaseManager.Instance.DialogDatabase.Get(id);
            if (node == null)
            {
                Debug.LogError($"[DialoguePanel] 找不到节点 {id}，对话结束。");
                UIManager.Instance.Close(this);
                return;
            }

            currentNode = node;
            SetText(speakerText, node.Speaker);
            SetText(lineText, node.Text);

            ClearOptions();

            List<DialogOption> options = DatabaseManager.Instance.DialogOptionDatabase.GetOptions(id);
            bool hasOptions = options != null && options.Count > 0;
            if (nextButton != null) nextButton.gameObject.SetActive(!hasOptions);
            if (hasOptions) CreateOptions(options);
        }

        private void CreateOptions(List<DialogOption> options)
        {
            if (optionRoot == null || optionButtonTemplate == null)
            {
                Debug.LogError("[DialoguePanel] 选项按钮没有配置，本节点只显示文本。");
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                DialogOption option = options[i];
                Button button = Instantiate(optionButtonTemplate, optionRoot);
                button.gameObject.SetActive(true);

                SetText(button.GetComponentInChildren<TMP_Text>(true), option.Text);

                int nextId = option.NextId;
                button.onClick.AddListener(() => Advance(nextId));
                optionButtons.Add(button);
            }
        }

        private void ClearOptions()
        {
            for (int i = 0; i < optionButtons.Count; i++)
            {
                if (optionButtons[i] != null) Destroy(optionButtons[i].gameObject);
            }

            optionButtons.Clear();
        }

        private void Advance(int nextId)
        {
            if (nextId == 0)
            {
                UIManager.Instance.Close(this);
                return;
            }

            ShowNode(nextId);
        }

        private void SetText(TMP_Text text, string value)
        {
            if (text == null) return;

            text.text = value;
        }
    }
}
