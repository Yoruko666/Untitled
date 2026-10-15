#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNTITLED_DEVELOPER_TOOLS)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Untitled
{
    // No bootstrap, save IO, scene loading or secondary game state is owned by this service.
    public sealed class DebugCommandService
    {
        private const int LogCapacity = 100;
        private static DebugCommandService current;
        private readonly List<DebugCommandResult> history = new List<DebugCommandResult>();
        public static DebugCommandService Current => current ?? (current = new DebugCommandService());
        public IReadOnlyList<DebugCommandResult> History { get; }
        public event Action<DebugCommandResult> Executed;
        private bool executing;

        private DebugCommandService() => History = history.AsReadOnly();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => current = null;

        public bool TryInspect(out DebugStateSnapshot snapshot, out string reason)
        {
            snapshot = null;
            if (!TryGetSession(out var manager, out reason)) return false;
            snapshot = new DebugStateSnapshot(manager, SceneManager.GetActiveScene().path);
            return true;
        }

        public DebugCommandResult Execute(DebugCommandRequest request)
        {
            if (executing)
            {
                var rejected = new DebugCommandResult(request?.ToString() ?? "null", DebugCommandStatus.Failed,
                    "命令正在执行；不允许事件回调重入调试命令。", null);
                Record(rejected, false);
                return rejected;
            }
            executing = true;
            try
            {
                DebugCommandStatus status;
                string message;
                try { status = ExecuteCore(request, out message); }
                catch (Exception exception)
                {
                    status = DebugCommandStatus.Failed;
                    message = "命令异常：" + exception.Message + "。事件回调可能已收到状态变化，请检查当前状态。";
                    Debug.LogException(exception);
                }
                TryInspect(out var snapshot, out _);
                var result = new DebugCommandResult(request?.ToString() ?? "null", status, message, snapshot);
                Record(result, true);
                return result;
            }
            finally { executing = false; }
        }

        private void Record(DebugCommandResult result, bool notify)
        {
            if (history.Count == LogCapacity) history.RemoveAt(0);
            history.Add(result);
            Debug.Log($"[DeveloperTools] {result.Command}: {result.Status} — {result.Message}");
            if (notify) Executed?.Invoke(result);
        }

        private static DebugCommandStatus ExecuteCore(DebugCommandRequest request, out string message)
        {
            if (request == null) return Fail("命令不能为空。", out message);
            if (!Enum.IsDefined(typeof(DebugCommandKind), request.Kind)) return Fail("未知调试命令。", out message);
            if (!TryGetSession(out var manager, out message)) return DebugCommandStatus.Failed;
            var game = manager.GameData;
            switch (request.Kind)
            {
                case DebugCommandKind.GetState:
                case DebugCommandKind.GetInventory:
                    message = "已读取当前共享状态和库存。";
                    return DebugCommandStatus.Succeeded;
                case DebugCommandKind.AddMoney:
                    if (request.Value <= 0) return Fail("增加金币必须为正整数。", out message);
                    if ((long)game.Money + request.Value > int.MaxValue) return Fail("金币数量会溢出。", out message);
                    manager.MarkDeveloperStateModified();
                    if (!game.TryAddMoney(request.Value)) return Fail("现有金币服务拒绝增加金币。", out message);
                    message = $"金币已增加至 {game.Money}；已发送原 UI 更新事件。";
                    return DebugCommandStatus.Succeeded;
                case DebugCommandKind.SetMoney:
                    if (request.Value < 0) return Fail("金币不能为负数。", out message);
                    if (request.Value != game.Money)
                    {
                        manager.MarkDeveloperStateModified();
                        bool accepted = request.Value > game.Money
                            ? game.TryAddMoney(request.Value - game.Money)
                            : game.TrySpendMoney(game.Money - request.Value);
                        if (!accepted) return Fail("现有金币服务拒绝设置金币。", out message);
                    }
                    message = $"金币为 {game.Money}。";
                    return DebugCommandStatus.Succeeded;
                case DebugCommandKind.GiveItem:
                case DebugCommandKind.RemoveItem:
                    if (request.ItemId <= 0 || request.Value <= 0) return Fail("物品 ID 和数量必须为正整数。", out message);
                    if (!DatabaseManager.TryGet(out var database) || database.ItemDatabase == null)
                        return Unavailable("物品数据库未初始化。", out message);
                    if (!database.ItemDatabase.TryGet(request.ItemId, out _)) return Fail($"物品 ID {request.ItemId} 不存在。", out message);
                    if (request.Kind == DebugCommandKind.RemoveItem)
                    {
                        if (!game.InventoryData.HasItem(request.ItemId, request.Value)) return Fail("库存数量不足。", out message);
                        manager.MarkDeveloperStateModified();
                        if (!game.InventoryData.ConsumeItem(request.ItemId, request.Value)) return Fail("背包服务拒绝删除物品。", out message);
                    }
                    else
                    {
                        // Protect ordinary saves before Changed subscribers can attempt a save.
                        bool wasModified = manager.DeveloperStateModified;
                        manager.MarkDeveloperStateModified();
                        if (!game.InventoryData.TryAddItem(request.ItemId, request.Value))
                        {
                            manager.RestoreDeveloperSaveGuard(wasModified);
                            return Fail("背包服务拒绝发放：容量不足、堆叠配置无效或数量溢出。", out message);
                        }
                    }
                    message = $"物品 {request.ItemId} 当前数量：{game.InventoryData.GetItemCount(request.ItemId)}。";
                    return DebugCommandStatus.Succeeded;
                case DebugCommandKind.ClearInventory:
                    if (!request.Confirmed) return Confirm("清空会删除本次会话全部仓库物品，请明确确认。", out message);
                    if (game.InventoryData.Items.Count > 0)
                    {
                        manager.MarkDeveloperStateModified();
                        game.InventoryData.Clear();
                    }
                    message = "当前会话仓库已清空；探索包裹和存档文件未修改。";
                    return DebugCommandStatus.Succeeded;
                case DebugCommandKind.SetDay:
                case DebugCommandKind.SetDayMetadata:
                    if (request.Value < 1 || request.Value > 7) return Fail("Day 必须为 1–7。", out message);
                    if (request.Kind == DebugCommandKind.SetDay)
                        return Unavailable("缺少七日剧情/订单初始化与 Day 就绪验证服务；不能准备可游玩的指定 Day。", out message);
                    if (!request.Confirmed) return Confirm("仅修改共享 Day 数据，不准备剧情、订单、任务或场景，请明确确认。", out message);
                    if (game.Day != request.Value)
                    {
                        manager.MarkDeveloperStateModified();
                        if (!game.SetDay(request.Value)) return Fail("现有状态服务拒绝设置 Day。", out message);
                    }
                    message = $"共享 Day 数据为 {game.Day}；剧情/订单/任务依赖未准备，不能视为该天可正常测试。";
                    return DebugCommandStatus.Succeeded;
                case DebugCommandKind.SetPhase:
                    if (!Enum.IsDefined(typeof(GamePhase), request.Phase)) return Fail("无效游戏阶段。", out message);
                    if (!TryValidateScenePhase(request.Phase, out message)) return DebugCommandStatus.Unavailable;
                    if (game.Phase != request.Phase)
                    {
                        manager.MarkDeveloperStateModified();
                        if (!game.SetPhase(request.Phase)) return Fail("现有状态服务拒绝设置阶段。", out message);
                    }
                    message = $"共享阶段已与当前场景对齐为 {game.Phase}；未运行日夜流程或结算。";
                    return DebugCommandStatus.Succeeded;
                default: return Fail("命令尚未实现。", out message);
            }
        }

        private static bool TryValidateScenePhase(GamePhase phase, out string reason)
        {
            var scene = SceneManager.GetActiveScene();
            reason = null;
            if (!scene.IsValid() || !scene.isLoaded) { reason = "当前场景尚未加载完成。"; return false; }
            if (phase == GamePhase.Settlement) { reason = "功能不可用：缺少结算流程与结算场景入口。"; return false; }
            int shops = 0, caves = 0;
            bool shopReady = false, caveReady = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var shop in root.GetComponentsInChildren<ShopSceneBindings>(true))
                {
                    shops++;
                    shopReady |= shop.isActiveAndEnabled && shop.Player != null && shop.Player.isActiveAndEnabled
                        && shop.PlayerSpawn != null;
                }
                foreach (var cave in root.GetComponentsInChildren<AdventureCaveSession>(true))
                {
                    caves++;
                    caveReady |= cave.isActiveAndEnabled && cave.Player != null && cave.Player.isActiveAndEnabled
                        && cave.FollowCamera != null && cave.FollowCamera.isActiveAndEnabled;
                }
            }
            bool valid = phase == GamePhase.Shop ? shops == 1 && caves == 0 && shopReady
                : caves == 1 && shops == 0 && caveReady;
            if (!valid) reason = "功能不可用：当前场景缺少唯一且就绪的对应玩法入口/玩家；请先在 Editor 加载相应场景。第一阶段不执行场景跳转。";
            return valid;
        }

        private static bool TryGetSession(out GameManager manager, out string reason)
        {
            manager = null;
            reason = null;
            if (!Application.isPlaying) { reason = "请进入 Play Mode 后执行调试命令。"; return false; }
            if (!GameManager.TryGet(out manager) || manager.GameData == null)
            { reason = "GameSession 不存在：GameManager/GameData 未由 Bootstrap 初始化。"; return false; }
            return true;
        }

        private static DebugCommandStatus Fail(string reason, out string message) { message = reason; return DebugCommandStatus.Failed; }
        private static DebugCommandStatus Unavailable(string reason, out string message) { message = "功能不可用：" + reason; return DebugCommandStatus.Unavailable; }
        private static DebugCommandStatus Confirm(string reason, out string message) { message = reason; return DebugCommandStatus.ConfirmationRequired; }
    }
}
#endif
