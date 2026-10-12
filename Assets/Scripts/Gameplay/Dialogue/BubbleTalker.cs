using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Untitled.Config;

namespace Untitled
{
    public class BubbleTalker : MonoBehaviour
    {
        [FormerlySerializedAs("view")]
        [SerializeField] private BubbleView viewPrefab;
        [SerializeField] private List<int> bubbleIds = new();
        [SerializeField] private float heightOffset = 2f;
        [SerializeField] private float showTime = 3f;
        [SerializeField] private float interval = 4f;
        [SerializeField] private Camera targetCamera;

        private BubbleView view;
        private int index;
        private float timer;
        private bool showing;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;

            timer = interval;
        }

        private void Start()
        {
            CreateView();
        }

        private void OnDestroy()
        {
            if (view != null) Destroy(view.gameObject);
        }

        private void LateUpdate()
        {
            if (view == null || targetCamera == null) return;

            Vector3 worldPosition = transform.position + Vector3.up * heightOffset;
            view.SetScreenPosition(targetCamera.WorldToScreenPoint(worldPosition));
        }

        private void Update()
        {
            if (view == null || bubbleIds.Count == 0) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            if (showing) EndBubble();
            else BeginBubble();
        }

        private void BeginBubble()
        {
            int id = bubbleIds[index];
            Bubble bubble = DatabaseManager.Instance.BubbleDatabase.Get(id);

            if (bubble == null)
            {
                Debug.LogError($"[BubbleTalker] 找不到 id {id} 的气泡，已跳过。");
                Next();
                return;
            }

            view.Show(bubble.Text);
            showing = true;
            timer = showTime;
        }

        private void EndBubble()
        {
            view.Hide();
            showing = false;
            Next();
        }

        private void Next()
        {
            index = (index + 1) % bubbleIds.Count;
            timer = interval;
        }

        private void CreateView()
        {
            if (viewPrefab == null)
            {
                Debug.LogError("[BubbleTalker] 没有配置气泡预制体，气泡不会显示。", this);
                return;
            }

            Transform root = UIManager.Instance != null ? UIManager.Instance.Root : null;
            if (root == null)
            {
                Debug.LogError("[BubbleTalker] 找不到 UI 画布，气泡不会显示。", this);
                return;
            }

            view = Instantiate(viewPrefab, root);
            view.name = "Bubble";
            view.transform.SetAsFirstSibling();
            view.Hide();
        }
    }
}
