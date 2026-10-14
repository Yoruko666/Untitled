using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIInventoryWindow : PanelBase
    {
        public UIInventoryPanel View;
        public UIButton CloseButton;
        private IUIInventoryReadModel model;
        protected override void OnInit() => CloseButton.Clicked.AddListener(Close);
        protected override void OnShow(object data)
        {
            Unsubscribe();
            model = data as IUIInventoryReadModel;
            if (model != null) { model.Changed += Render; Render(model.Current); }
            else Render(data as UIInventorySnapshot);
        }
        protected override void OnHide() => Unsubscribe();
        private void Render(UIInventorySnapshot value) => View.Bind(value);
        private void Unsubscribe() { if (model != null) model.Changed -= Render; model = null; }
        private void Close() => UIManager.Instance.Close(this);
        private void OnDestroy() { Unsubscribe(); if (CloseButton != null) CloseButton.Clicked.RemoveListener(Close); }
    }
}
