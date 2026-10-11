using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Untitled
{
    public abstract class DatabaseBase
    {
        public abstract int Count { get; }

        public abstract void Load(string address);
    }

    public abstract class DatabaseBase<TRow> : DatabaseBase where TRow : class
    {
        private readonly Dictionary<int, TRow> lookup = new();

        public IReadOnlyDictionary<int, TRow> All => lookup;

        public override int Count => lookup.Count;

        protected abstract int KeyOf(TRow row);

        protected abstract IEnumerable<TRow> Parse(byte[] bytes);

        public override void Load(string address)
        {
            byte[] bytes = LoadBytes(address);

            lookup.Clear();
            foreach (TRow row in Parse(bytes))
            {
                int key = KeyOf(row);
                if (lookup.ContainsKey(key))
                {
                    Debug.LogError($"[{GetType().Name}] 主键 {key} 重复，已保留先出现的一条。");
                    continue;
                }

                lookup.Add(key, row);
            }
        }

        public TRow Get(int key)
        {
            return TryGet(key, out TRow row) ? row : null;
        }

        public bool TryGet(int key, out TRow row)
        {
            return lookup.TryGetValue(key, out row);
        }

        private static byte[] LoadBytes(string address)
        {
            AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(address);
            TextAsset asset = handle.WaitForCompletion();

            if (asset == null)
            {
                Debug.LogError($"[DatabaseBase] 加载配表失败：{address}");
                if (handle.IsValid()) Addressables.Release(handle);
                return System.Array.Empty<byte>();
            }

            byte[] bytes = asset.bytes;
            if (handle.IsValid()) Addressables.Release(handle);
            return bytes;
        }
    }
}
