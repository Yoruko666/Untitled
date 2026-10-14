using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Untitled.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public sealed class UIScrollFollowSelection : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        private GameObject previous;
        public void Configure(ScrollRect view) => scroll = view;
        private void OnEnable() => previous = null;
        private void Update()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == previous) return;
            previous = selected;
            if (scroll == null || selected == null || !selected.transform.IsChildOf(scroll.content)
                || !selected.TryGetComponent<RectTransform>(out var rect)) return;
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            float bottom = scroll.viewport.InverseTransformPoint(corners[0]).y;
            float top = scroll.viewport.InverseTransformPoint(corners[2]).y;
            var bounds = scroll.viewport.rect;
            float shift = bottom < bounds.yMin ? bounds.yMin - bottom : top > bounds.yMax ? bounds.yMax - top : 0;
            var position = scroll.content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + shift, 0, Mathf.Max(0, scroll.content.rect.height - bounds.height));
            scroll.content.anchoredPosition = position;
        }
    }
}
