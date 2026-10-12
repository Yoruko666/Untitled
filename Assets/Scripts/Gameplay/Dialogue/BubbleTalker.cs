using System.Collections.Generic;
using UnityEngine;
using Untitled.Config;

namespace Untitled
{
    public class BubbleTalker : MonoBehaviour
    {
        [SerializeField] private BubbleView view;
        [SerializeField] private List<int> bubbleIds = new();
        [SerializeField] private float heightOffset = 2f;
        [SerializeField] private float showTime = 3f;
        [SerializeField] private float interval = 4f;

        private int index;
        private float timer;
        private bool showing;

        private void Awake()
        {
            if (view != null)
            {
                Vector3 localPosition = view.transform.localPosition;
                localPosition.y = heightOffset;
                view.transform.localPosition = localPosition;
                view.Hide();
            }

            timer = interval;
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
    }
}
