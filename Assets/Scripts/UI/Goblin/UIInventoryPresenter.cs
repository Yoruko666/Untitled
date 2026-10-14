using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIInventoryPresenter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour source;
        [SerializeField] private UIInventoryPanel view;
        private IUIInventoryReadModel model;
        private bool listening;
        public void Configure(MonoBehaviour provider, UIInventoryPanel panel) { source = provider; view = panel; }
        public void SetSource(IUIInventoryReadModel provider)
        { Unsubscribe(); model = provider; if (isActiveAndEnabled) Subscribe(); }
        private void OnEnable()
        {
            if (model == null) model = source as IUIInventoryReadModel;
            if (source != null && model == null) Debug.LogError("Inventory source must implement IUIInventoryReadModel.", this);
            Subscribe();
        }
        private void OnDisable() => Unsubscribe();
        private void Subscribe() { if (model == null || listening) return; model.Changed += Render; listening = true; Render(model.Current); }
        private void Unsubscribe() { if (model != null && listening) model.Changed -= Render; listening = false; }
        private void Render(UIInventorySnapshot value) { if (view != null && value != null) view.Bind(value); }
    }
}
