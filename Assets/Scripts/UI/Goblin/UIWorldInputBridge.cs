using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIWorldInputBridge : MonoBehaviour
    {
        [SerializeField] private ShopFirstPersonController shopPlayer;
        private UIManager manager;
        private bool blocked;
        private CursorLockMode previousLock;
        private bool previousVisible;
        public void Configure(ShopFirstPersonController player) => shopPlayer = player;
        private void OnEnable()
        {
            if (manager != null) { manager.ModalStateChanged += OnModal; OnModal(manager.HasOpenPanels); }
        }
        private void Start()
        {
            if (!UIManager.TryGet(out manager)) return;
            manager.SetRoot(transform);
            manager.ModalStateChanged += OnModal;
            OnModal(manager.HasOpenPanels);
        }
        private void OnDisable()
        {
            if (manager != null) manager.ModalStateChanged -= OnModal;
            OnModal(false);
        }
        private void OnModal(bool open)
        {
            if (blocked == open) return;
            blocked = open;
            if (shopPlayer != null) { shopPlayer.SetUIBlocked(open); return; }
            if (open) { previousLock = Cursor.lockState; previousVisible = Cursor.visible; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else { Cursor.lockState = previousLock; Cursor.visible = previousVisible; }
        }
    }
}
