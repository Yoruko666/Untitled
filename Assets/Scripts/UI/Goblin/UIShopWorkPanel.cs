using UnityEngine;
using UnityEngine.Events;

namespace Untitled.UI
{
    public sealed class UIShopWorkPanel : PanelBase
    {
        public UIOrderCard View;
        public UIButton CloseButton;
        public UnityEvent<string> WorkRequested = new UnityEvent<string>();
        protected override void OnInit() { CloseButton.Clicked.AddListener(Close); View.ActionRequested.AddListener(Request); }
        protected override void OnShow(object data) => View.Bind(data as UIOrderData);
        private void Request(string id) => WorkRequested.Invoke(id);
        private void Close() => UIManager.Instance.Close(this);
        private void OnDestroy()
        {
            if (CloseButton != null) CloseButton.Clicked.RemoveListener(Close);
            if (View != null) View.ActionRequested.RemoveListener(Request);
        }
    }
}
