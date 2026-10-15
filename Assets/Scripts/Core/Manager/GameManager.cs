using System.IO;
using UnityEngine;

namespace Untitled
{
    public class GameManager : ManagerBase<GameManager>
    {
        public GameData GameData { get; private set; }
        public GameSession Session { get; private set; }
        public bool HasLoadedSave { get; private set; }
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
            Session = new GameSession(GameData);
            saveStore = new GameSaveStore(configuredSavePath ?? Path.Combine(Application.persistentDataPath, "shared-game-v1.json"));
            if (File.Exists(saveStore.Path) && !TryLoad(out string error)) Debug.LogWarning("[GameManager] 加载失败，使用新游戏状态：" + error);
        }

        // Bootstrap supplies session configuration once, before Initialize.
        public void Configure(InventorySettings settings, string savePath = null)
        {
            inventorySettings = settings;
            configuredSavePath = savePath;
        }

#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNTITLED_DEVELOPER_TOOLS)
        public bool DeveloperStateModified { get; private set; }
        internal void MarkDeveloperStateModified() => DeveloperStateModified = true;
        // Only undo a guard raised for an atomic operation rejected before any event fired.
        internal void RestoreDeveloperSaveGuard(bool previous) => DeveloperStateModified = previous;
#endif

        public bool TrySave(out string error)
        {
            if (!Session.IsReady) { error = "存档恢复尚未完成。"; return false; }
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNTITLED_DEVELOPER_TOOLS)
            if (DeveloperStateModified)
            {
                error = "调试会话禁止使用普通保存入口。专用测试槽位尚未实现；请停止 Play Mode 后重新开始正常会话。";
                return false;
            }
#endif
            return saveStore.TrySave(GameData.Capture(), out error);
        }

        public bool TryLoad(out string error)
        {
            if (!Session.IsReady) { error = "存档恢复尚未完成。"; return false; }
            if (!saveStore.TryLoad(out var saved, out error)) return false;
            if (!GameData.TryRestore(saved, out int skipped))
            {
                error = "存档版本或状态字段无效，当前状态保持。";
                return false;
            }
            if (skipped > 0) Debug.LogWarning($"[GameManager] 恢复时跳过 {skipped} 条无效物品记录。");
            HasLoadedSave = true;
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
