using UnityEngine;

namespace Untitled
{
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
        }

        private void RegisterManager<T>(ManagerBase<T> manager) where T : ManagerBase<T>
        {
            manager.Initialize();
        }
    }
}