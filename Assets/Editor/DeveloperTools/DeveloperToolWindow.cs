using UnityEditor;
using UnityEngine;

namespace Untitled.Editor
{
    public sealed class DeveloperToolWindow : EditorWindow
    {
        [SerializeField] private int day = 1;
        [SerializeField] private int money = 100;
        [SerializeField] private int itemId = 1;
        [SerializeField] private int quantity = 1;
        [SerializeField] private GamePhase phase;
        private Vector2 scroll;
        private bool inventoryExpanded = true;
        private readonly DebugLogPanel log = new DebugLogPanel();
        private DebugCommandResult lastResult;

        [MenuItem("Untitled/Developer Tools/Debug Toolkit")]
        public static void Open() => GetWindow<DeveloperToolWindow>("哥布林奶茶店 · Debug");

        private void OnEnable()
        {
            minSize = new Vector2(440, 620);
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update += OnEditorUpdate;
        }
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.update -= OnEditorUpdate;
        }
        private double nextRepaint;
        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < nextRepaint) return;
            nextRepaint = EditorApplication.timeSinceStartup + 0.5;
            Repaint();
        }
        private void OnPlayMode(PlayModeStateChange state) { lastResult = null; Repaint(); }

        private void OnGUI()
        {
            var service = DebugCommandService.Current;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Developer Debug Toolkit · 第一阶段", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("命令仅修改本次运行的共享数据。首次修改后普通保存入口被锁住；停止 Play Mode 后重建会话解除。", MessageType.Info);
            bool ready = service.TryInspect(out var state, out string reason);
            if (!ready) EditorGUILayout.HelpBox(reason, MessageType.Warning);
            else DrawState(state);

            using (new EditorGUI.DisabledScope(!ready || EditorApplication.isPaused || EditorApplication.isCompiling))
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("游戏流程", EditorStyles.boldLabel);
                day = EditorGUILayout.IntField("目标 Day（1–7）", day);
                if (GUILayout.Button("准备指定 Day")) Execute(new DebugCommandRequest(DebugCommandKind.SetDay, day));
                EditorGUILayout.HelpBox("缺少七日剧情与订单就绪服务。准备 Day 会报告功能不可用；以下按钮仅用于检查共享数据与 HUD。", MessageType.Warning);
                if (GUILayout.Button("仅修改 Day 数据…"))
                    Execute(new DebugCommandRequest(DebugCommandKind.SetDayMetadata, day, confirmed:
                        EditorUtility.DisplayDialog("仅修改 Day 数据", "不准备剧情、订单、任务或场景，不能视为该天可正常测试。继续？", "修改数据", "取消")));
                phase = (GamePhase)EditorGUILayout.EnumPopup("目标阶段", phase);
                if (GUILayout.Button("与当前场景对齐阶段")) Execute(new DebugCommandRequest(DebugCommandKind.SetPhase, phase: phase));
                EditorGUILayout.LabelField("检查唯一店铺/地下入口及玩家；场景跳转与结算流程留待后续。", EditorStyles.wordWrappedMiniLabel);

                GUILayout.Space(8);
                EditorGUILayout.LabelField("金币", EditorStyles.boldLabel);
                money = EditorGUILayout.IntField("数量", money);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("增加金币")) Execute(new DebugCommandRequest(DebugCommandKind.AddMoney, money));
                if (GUILayout.Button("设置金币")) Execute(new DebugCommandRequest(DebugCommandKind.SetMoney, money));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(8);
                EditorGUILayout.LabelField("仓库背包", EditorStyles.boldLabel);
                itemId = EditorGUILayout.IntField("物品 ID（现有表）", itemId);
                quantity = EditorGUILayout.IntField("数量", quantity);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("发放道具")) Execute(new DebugCommandRequest(DebugCommandKind.GiveItem, quantity, itemId));
                if (GUILayout.Button("删除道具")) Execute(new DebugCommandRequest(DebugCommandKind.RemoveItem, quantity, itemId));
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("清空本次测试仓库…"))
                    Execute(new DebugCommandRequest(DebugCommandKind.ClearInventory, confirmed:
                        EditorUtility.DisplayDialog("清空本次测试仓库", "删除当前会话全部仓库物品，不能撤销。继续？", "清空", "取消")));
                if (GUILayout.Button("读取库存")) Execute(new DebugCommandRequest(DebugCommandKind.GetInventory));
            }

            if (lastResult != null)
                EditorGUILayout.HelpBox($"{lastResult.Status}：{lastResult.Message}", lastResult.Success ? MessageType.Info : MessageType.Warning);
            GUILayout.Space(8);
            EditorGUILayout.HelpBox("订单/任务/探索结果：缺少业务服务，功能不可用。F1 游戏面板、场景跳转、测试预设、测试存档按第二/三阶段实现。", MessageType.Info);
            log.Draw(service);
            EditorGUILayout.EndScrollView();
        }

        private void DrawState(DebugStateSnapshot state)
        {
            EditorGUILayout.LabelField($"Day {state.Day} / {state.Phase} / 金币 {state.Money}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前场景", state.Scene);
            EditorGUILayout.LabelField("普通保存", state.NormalSaveBlocked ? "已锁住（调试会话）" : "尚未修改状态");
            EditorGUILayout.LabelField("占用格数", $"{state.UsedSlots} / {(state.Capacity == 0 ? "无限" : state.Capacity.ToString())}");
            EditorGUILayout.LabelField("接受订单实例 ID", state.AcceptedOrders.Count == 0 ? "无" : string.Join(", ", state.AcceptedOrders));
            EditorGUILayout.LabelField("剧情标记", state.StoryFlags.Count == 0 ? "无" : string.Join(", ", state.StoryFlags));
            inventoryExpanded = EditorGUILayout.Foldout(inventoryExpanded, $"当前库存（{state.Items.Count} 种）", true);
            if (!inventoryExpanded) return;
            foreach (var item in state.Items) EditorGUILayout.LabelField($"ID {item.Id}", $"× {item.Count}");
        }

        private void Execute(DebugCommandRequest request)
        {
            // Cancel still passes through the service's confirmation gate and cannot mutate state.
            lastResult = DebugCommandService.Current.Execute(request);
            Repaint();
        }
    }
}
