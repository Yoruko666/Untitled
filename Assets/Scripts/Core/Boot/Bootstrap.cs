using UnityEngine;

namespace Untitled
{
    [DefaultExecutionOrder(-1000)]
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private InventorySettings inventorySettings;

        private void Awake()
        {
            RegisterManager(new DatabaseManager());
            var game = new GameManager();
            game.Configure(inventorySettings);
            RegisterManager(game);
            RegisterManager(new InputManager());
            RegisterManager(new UIManager());
            RegisterManager(new InteractManager());
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNTITLED_DEVELOPER_TOOLS)
            RegisterManager(new DebugManager());
            if (DebugManager.TryGet(out var debug)) debug.EnsureSceneOverlay();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            if (InputManager.TryGet(out var input)) input.Dispose();
            if (UIManager.TryGet(out var ui)) ui.Dispose();
            if (InteractManager.TryGet(out var interactions)) interactions.Dispose();
            EventCenter.Clear();
            DatabaseManager.ResetInstance(); GameManager.ResetInstance(); InputManager.ResetInstance();
            UIManager.ResetInstance(); InteractManager.ResetInstance();
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNTITLED_DEVELOPER_TOOLS)
            DebugManager.ResetInstance();
#endif
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
