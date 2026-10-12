using TMPro;
using UnityEngine;

namespace Untitled
{
    /// <summary>挂在 Screen Space - Camera 画布下的气泡：按屏幕坐标跟随世界目标，尺寸随文本自适应。</summary>
    public class BubbleView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform background;
        [SerializeField] private Canvas canvas;
        [SerializeField] private float maxWidth = 240f;
        [SerializeField] private Vector2 padding = new(24f, 16f);

        private RectTransform rectTransform;
        private bool showing;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            if (background == null) background = rectTransform;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
        }

        public void Show(string value)
        {
            Resize(value);

            showing = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            showing = false;
            gameObject.SetActive(false);
        }

        /// <summary>用世界坐标投影出的屏幕坐标定位；目标落在相机后方时自动隐藏。</summary>
        public void SetScreenPosition(Vector3 screenPosition)
        {
            if (!showing) return;

            bool inFront = screenPosition.z > 0f;
            if (gameObject.activeSelf != inFront) gameObject.SetActive(inFront);
            if (!inFront) return;

            if (rectTransform == null || canvas == null) return;

            Canvas root = canvas.rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            RectTransform canvasRect = root.transform as RectTransform;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out Vector2 localPoint))
            {
                rectTransform.localPosition = localPoint;
            }
        }

        /// <summary>文本宽度超出 maxWidth 时换行，背景按文本尺寸加内边距撑开。</summary>
        private void Resize(string value)
        {
            if (text == null) return;

            float width = Mathf.Min(text.GetPreferredValues(value).x, maxWidth);
            float height = text.GetPreferredValues(value, width, 0f).y;

            width = Mathf.Ceil(width);
            height = Mathf.Ceil(height);

            text.text = value;
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            if (background != null)
            {
                background.sizeDelta = new Vector2(width + padding.x * 2f, height + padding.y * 2f);
            }
        }
    }
}
