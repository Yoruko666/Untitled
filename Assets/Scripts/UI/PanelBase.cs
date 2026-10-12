using UnityEngine;

namespace Untitled
{
    public abstract class PanelBase : MonoBehaviour
    {
        [SerializeField] private bool closeOnEsc = true;
        [SerializeField] private bool pauseWorld = true;

        public bool IsOpen { get; private set; }

        public object Data { get; private set; }

        public string PanelName { get; internal set; }

        public bool CloseOnEsc => closeOnEsc;

        public bool PauseWorld => pauseWorld;

        private bool initialized;

        protected virtual void OnInit() { }

        protected virtual void OnShow(object data) { }

        protected virtual void OnHide() { }

        internal void ExecuteInit()
        {
            if (initialized) return;
            initialized = true;
            OnInit();
        }

        internal void ExecuteShow(object data)
        {
            Data = data;
            IsOpen = true;
            gameObject.SetActive(true);
            OnShow(data);
        }

        internal void ExecuteHide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            OnHide();
            gameObject.SetActive(false);
        }
    }
}
