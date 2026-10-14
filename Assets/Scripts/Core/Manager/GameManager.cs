using System.IO;
using UnityEngine;

namespace Untitled
{
    public class GameManager : ManagerBase<GameManager>
    {
        public GameData GameData { get; private set; }
        public string SavePath => saveStore?.Path;
        private InventorySettings inventorySettings;
        private string configuredSavePath;
        private GameSaveStore saveStore;

        public bool IsPaused { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            DatabaseManager.TryGet(out var database);
            GameData = new GameData(new ItemCatalog(database?.ItemDatabase, inventorySettings), inventorySettings != null ? inventorySettings.Capacity : 0);
            saveStore = new GameSaveStore(configuredSavePath ?? Path.Combine(Application.persistentDataPath, "shared-game-v1.json"));
            if (File.Exists(saveStore.Path) && !TryLoad(out string error)) Debug.LogWarning("[GameManager] 加载失败，使用新游戏状态：" + error);
        }

        // Bootstrap supplies session configuration once, before Initialize.
        public void Configure(InventorySettings settings, string savePath = null)
        {
            inventorySettings = settings;
            configuredSavePath = savePath;
        }

        public bool TrySave(out string error) => saveStore.TrySave(GameData.Capture(), out error);

        public bool TryLoad(out string error)
        {
            if (!saveStore.TryLoad(out var saved, out error)) return false;
            if (!GameData.TryRestore(saved, out int skipped))
            {
                error = "存档版本或状态字段无效，当前状态保持。";
                return false;
            }
            if (skipped > 0) Debug.LogWarning($"[GameManager] 恢复时跳过 {skipped} 条无效物品记录。");
            return true;
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }
    }
}
