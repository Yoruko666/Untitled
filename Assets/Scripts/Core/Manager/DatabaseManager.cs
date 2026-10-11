using System;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled
{
    public class DatabaseManager : ManagerBase<DatabaseManager>
    {
        private readonly Dictionary<Type, DatabaseBase> databases = new();

        public ItemDatabase ItemDatabase { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            ItemDatabase = Register(new ItemDatabase(), DatabasePath.ItemDatabase);
        }

        public T Register<T>(T database, string address) where T : DatabaseBase
        {
            if (database == null)
            {
                Debug.LogError("[DatabaseManager] 注册的配表为空，已跳过。");
                return null;
            }

            Type type = database.GetType();
            if (databases.ContainsKey(type))
            {
                Debug.LogError($"[DatabaseManager] 配表 {type.Name} 重复注册，已跳过。");
                return null;
            }

            database.Load(address);
            databases.Add(type, database);
            Debug.Log($"[DatabaseManager] {type.Name} 已加载 {database.Count} 条（{address}）。");
            return database;
        }

        public T Get<T>() where T : DatabaseBase
        {
            if (databases.TryGetValue(typeof(T), out DatabaseBase database)) return (T)database;

            Debug.LogError($"[DatabaseManager] 配表 {typeof(T).Name} 尚未注册。");
            return null;
        }

        public bool TryGet<T>(out T database) where T : DatabaseBase
        {
            if (databases.TryGetValue(typeof(T), out DatabaseBase found))
            {
                database = (T)found;
                return true;
            }

            database = null;
            return false;
        }
    }
}
