using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled
{
    public abstract class ManagerBase<T> where T : ManagerBase<T>
    {
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError($"Instance of {typeof(T)} is not initialized.");
                }
                return instance;
            }
        }

        private static T instance;
        public static bool IsInitialized => instance != null;
        public static bool TryGet(out T value) { value = instance; return value != null; }
        public static void ResetInstance() => instance = null;

        public virtual void Initialize()
        {
            instance = (T)this;
        }
    }
}
