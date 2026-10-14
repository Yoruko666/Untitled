using UnityEngine;

namespace Untitled
{
    [DefaultExecutionOrder(-1000)]
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            RegisterManager(new DatabaseManager());
            RegisterManager(new GameManager());
            RegisterManager(new InputManager());
            RegisterManager(new UIManager());
            RegisterManager(new InteractManager());
            RegisterManager(new DebugManager());
            if (DebugManager.TryGet(out var debug)) debug.EnsureSceneOverlay();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            if (InputManager.TryGet(out var input)) input.Dispose();
            if (UIManager.TryGet(out var ui)) ui.Dispose();
            if (InteractManager.TryGet(out var interactions)) interactions.Dispose();
            EventCenter.Clear();
            DatabaseManager.ResetInstance(); GameManager.ResetInstance(); InputManager.ResetInstance();
            UIManager.ResetInstance(); InteractManager.ResetInstance(); DebugManager.ResetInstance();
            Time.timeScale = 1;
        }
        private void OnApplicationQuit()
        {
            if (InputManager.TryGet(out var input)) input.Dispose();
        }

        private void RegisterManager<T>(ManagerBase<T> manager) where T : ManagerBase<T>
        {
            if (!ManagerBase<T>.IsInitialized) manager.Initialize();
        }
    }
}
