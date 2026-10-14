using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIInventoryWindow : PanelBase
    {
        public UIInventoryPanel View;
        public UIButton CloseButton;
        protected override void OnInit() => CloseButton.Clicked.AddListener(Close);
        protected override void OnShow(object data) => View.Bind(data as UIInventorySnapshot);
        private void Close() => UIManager.Instance.Close(this);
        private void OnDestroy() { if (CloseButton != null) CloseButton.Clicked.RemoveListener(Close); }
    }
}
