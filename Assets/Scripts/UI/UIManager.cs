using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Untitled
{
    public class UIManager : ManagerBase<UIManager>
    {
        private readonly Dictionary<string, PanelBase> panels = new();
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> handles = new();
        private readonly List<PanelBase> stack = new();

        private Transform root;
        public event System.Action<bool> ModalStateChanged;
        public bool HasOpenPanels => stack.Count > 0;

        public IReadOnlyList<PanelBase> OpenPanels => stack;

        public PanelBase Top => stack.Count > 0 ? stack[stack.Count - 1] : null;

        public override void Initialize()
        {
            base.Initialize();
            EventCenter.Subscribe(GameEventType.ToggleESC, OnToggleEsc);
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public Transform Root
        {
            get
            {
                if (root == null)
                {
                    Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                    if (canvas != null) root = canvas.transform;
                }

                return root;
            }
        }

        public void SetRoot(Transform uiRoot)
        {
            root = uiRoot;
        }

        public PanelBase Open(string panelName, object data = null)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                Debug.LogError("[UIManager] 面板名为空，无法打开。");
                return null;
            }

            if (!panels.TryGetValue(panelName, out PanelBase instance))
            {
                instance = Create(panelName);
                if (instance == null) return null;
                panels[panelName] = instance;
            }

            instance.ExecuteInit();
            instance.ExecuteShow(data);
            MoveToTop(instance);
            NotifyPause();
            return instance;
        }

        public T Open<T>(string panelName, object data = null) where T : PanelBase
        {
            return Open(panelName, data) as T;
        }

        public void Close(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return;
            if (panels.TryGetValue(panelName, out PanelBase instance)) Close(instance);
        }

        public void Close(PanelBase panel)
        {
            if (panel == null || !panel.IsOpen) return;

            panel.ExecuteHide();
            if (stack.Remove(panel)) NotifyPause();
        }

        public bool CloseTop()
        {
            if (stack.Count == 0) return false;

            PanelBase panel = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            panel.ExecuteHide();
            NotifyPause();
            return true;
        }

        public void CloseAll()
        {
            if (stack.Count == 0) return;

            stack.Clear();
            foreach (PanelBase instance in panels.Values)
            {
                instance.ExecuteHide();
            }

            NotifyPause();
        }

        public bool IsOpen(string panelName)
        {
            return !string.IsNullOrEmpty(panelName)
                && panels.TryGetValue(panelName, out PanelBase instance)
                && instance.IsOpen;
        }

        public PanelBase Get(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return null;
            panels.TryGetValue(panelName, out PanelBase instance);
            return instance;
        }

        public T Get<T>() where T : PanelBase
        {
            foreach (PanelBase instance in panels.Values)
            {
                if (instance is T typed) return typed;
            }

            return null;
        }

        public void Unload(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return;

            bool stacked = false;

            if (panels.TryGetValue(panelName, out PanelBase instance))
            {
                panels.Remove(panelName);
                stacked = stack.Remove(instance);
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance.gameObject);
                }
            }

            if (handles.TryGetValue(panelName, out AsyncOperationHandle<GameObject> handle))
            {
                handles.Remove(panelName);
                if (handle.IsValid()) Addressables.ReleaseInstance(handle);
            }

            if (stacked) NotifyPause();
        }

        public void UnloadAll()
        {
            List<string> opened = new(panels.Keys);
            foreach (string panelName in opened)
            {
                Unload(panelName);
            }
        }

        public void Dispose()
        {
            EventCenter.Unsubscribe(GameEventType.ToggleESC, OnToggleEsc);
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            UnloadAll(); ModalStateChanged = null; root = null;
        }
        private void OnSceneUnloaded(Scene scene)
        {
            var removed = new List<string>();
            foreach (var entry in panels)
                if (entry.Value == null || entry.Value.gameObject.scene == scene) removed.Add(entry.Key);
            foreach (var name in removed) Unload(name);
            if (root == null || root.gameObject.scene == scene) root = null;
        }

        private void NotifyPause()
        {
            foreach (PanelBase panel in stack)
            {
                if (panel == null || !panel.PauseWorld) continue;

                GameManager.Instance.SetPaused(true);
                ModalStateChanged?.Invoke(HasOpenPanels);
                return;
            }

            GameManager.Instance.SetPaused(false);
            ModalStateChanged?.Invoke(HasOpenPanels);
        }

        private void OnToggleEsc()
        {
            PanelBase top = Top;
            if (top == null)
            {
                Open(UIPanel.Menu);
                return;
            }

            if (!top.CloseOnEsc) return;
            CloseTop();
        }

        private void MoveToTop(PanelBase panel)
        {
            stack.Remove(panel);
            stack.Add(panel);
            panel.transform.SetAsLastSibling();
        }

        private PanelBase Create(string panelName)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(panelName, Root);
            GameObject instanceObject = handle.WaitForCompletion();

            if (instanceObject == null)
            {
                Debug.LogError($"[UIManager] 加载面板失败：{panelName}（地址 \"{panelName}\"）。");
                if (handle.IsValid()) Addressables.ReleaseInstance(handle);
                return null;
            }

            PanelBase instance = instanceObject.GetComponent<PanelBase>();
            if (instance == null)
            {
                Debug.LogError($"[UIManager] 面板预制体根节点缺少 PanelBase 组件：{panelName}。");
                Addressables.ReleaseInstance(handle);
                return null;
            }

            instance.gameObject.SetActive(false);
            instance.name = panelName;
            instance.PanelName = panelName;
            handles[panelName] = handle;
            return instance;
        }
    }
}
