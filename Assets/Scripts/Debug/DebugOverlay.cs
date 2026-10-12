using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Untitled
{
    /// <summary>
    /// 开发者调试面板：IMGUI 绘制，F1 开关，由 DebugManager 在开发构建中创建。
    /// 热键直接查询 Input System 键盘，不改动游戏的 Input Actions。
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        private const int WindowId = 57291;
        private const float CoarseStep = 0.5f;

        private Rect windowRect = new(16f, 16f, 360f, 0f);
        private Font chineseFont;
        private GUIStyle windowStyle;
        private GUIStyle valueStyle;
        private GUIStyle labelStyle;
        private GUIStyle hintStyle;
        private bool showHint = true;

        private void Awake()
        {
            // IMGUI 默认字体不含中文字形，这里用系统字体绘制面板文字
            chineseFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 18);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard[Key.F1].wasPressedThisFrame) return;

            DebugManager.Instance.TogglePanel();
        }

        private void OnGUI()
        {
            DebugManager debug = DebugManager.Instance;
            if (debug == null) return;

            EnsureStyles();

            if (!debug.IsPanelOpen)
            {
                if (showHint) DrawHint();
                return;
            }

            showHint = false;
            windowRect = GUILayout.Window(WindowId, windowRect, DrawPanel, "开发者调试（F1 关闭）", windowStyle);
        }

        private void DrawPanel(int windowId)
        {
            DebugManager debug = DebugManager.Instance;

            GUILayout.BeginHorizontal();
            GUILayout.Label("玩家移速倍数", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("×" + debug.MoveSpeedMultiplier.ToString("0.00"), valueStyle);
            GUILayout.EndHorizontal();

            debug.MoveSpeedMultiplier = GUILayout.HorizontalSlider(
                debug.MoveSpeedMultiplier, DebugManager.MoveSpeedMultiplierMin, DebugManager.MoveSpeedMultiplierMax);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-0.5")) debug.AddMoveSpeedMultiplier(-CoarseStep);
            if (GUILayout.Button("+0.5")) debug.AddMoveSpeedMultiplier(CoarseStep);
            if (GUILayout.Button("重置")) debug.ResetMoveSpeedMultiplier();
            if (GUILayout.Button("关闭")) debug.ClosePanel();
            GUILayout.EndHorizontal();

            GUILayout.Label(DescribePlayerSpeed(debug), labelStyle);

            GUILayout.Space(12f);
            DrawCameraControls(debug);

            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
        }

        private void DrawCameraControls(DebugManager debug)
        {
            GUILayout.Label("摄像头调节（仅本次运行生效）", valueStyle);
            Camera sceneCamera = debug.SceneCamera;
            if (sceneCamera == null)
            {
                GUILayout.Label("未找到 MainCamera，请关闭并重新打开面板", labelStyle);
                return;
            }

            if (debug.IsCinemachineControlled)
            {
                DrawCinemachineControls(debug);
                return;
            }

            CameraController controller = debug.CameraController;
            if (controller != null && controller.isActiveAndEnabled)
            {
                controller.SetViewAngle(DrawSlider("俯仰角", controller.ViewAngle, 0f, 89f));
                controller.SetYawAngle(DrawSlider("水平旋转", controller.YawAngle, -180f, 180f));
                controller.SetDistance(DrawSlider("跟随距离", controller.Distance, 0.1f, 100f));
                Vector3 offset = controller.FocusOffset;
                offset.x = DrawSlider("焦点 X 偏移", offset.x, -30f, 30f);
                offset.y = DrawSlider("焦点 Y 高度", offset.y, -30f, 30f);
                offset.z = DrawSlider("焦点 Z 偏移", offset.z, -30f, 30f);
                controller.SetFocusOffset(offset);
                if (controller.Target == null)
                    GUILayout.Label("没有跟随目标，角度和位置调节暂不生效", labelStyle);
            }
            else
            {
                GUILayout.Label("CameraController 未启用，仅可调节缩放", labelStyle);
            }

            if (sceneCamera.orthographic)
                sceneCamera.orthographicSize = DrawSlider("正交范围（越小越近）", sceneCamera.orthographicSize, 0.1f, 50f);
            else
                sceneCamera.fieldOfView = DrawSlider("视野 FOV（越小越近）", sceneCamera.fieldOfView, 10f, 120f);

            if (GUILayout.Button("恢复原始镜头")) debug.ResetCamera();
        }

        private void DrawCinemachineControls(DebugManager debug)
        {
            CinemachineVirtualCamera virtualCamera = debug.VirtualCamera;
            if (virtualCamera == null)
            {
                GUILayout.Label("当前虚拟镜头暂不支持，请重新打开面板", labelStyle);
                return;
            }

            GUILayout.Label("控制镜头：" + virtualCamera.name, labelStyle);
            CinemachineFramingTransposer framing = debug.FramingTransposer;
            if (framing != null && virtualCamera.LookAt == null)
            {
                Vector3 angles = virtualCamera.transform.eulerAngles;
                float pitch = Mathf.DeltaAngle(0f, angles.x);
                float yaw = Mathf.DeltaAngle(0f, angles.y);
                float newPitch = DrawSlider("俯仰角", pitch, 0f, 89f);
                float newYaw = DrawSlider("水平旋转", yaw, -180f, 180f);
                if (newPitch != pitch || newYaw != yaw)
                    virtualCamera.transform.rotation = Quaternion.Euler(newPitch, newYaw, angles.z);

                framing.m_CameraDistance = DrawSlider("跟随距离", framing.m_CameraDistance, 0.1f, 100f);
                Vector3 offset = framing.m_TrackedObjectOffset;
                offset.x = DrawSlider("焦点 X 偏移", offset.x, -30f, 30f);
                offset.y = DrawSlider("焦点 Y 高度", offset.y, -30f, 30f);
                offset.z = DrawSlider("焦点 Z 偏移", offset.z, -30f, 30f);
                framing.m_TrackedObjectOffset = offset;
            }
            else
            {
                GUILayout.Label("此虚拟镜头仅支持缩放调节", labelStyle);
            }

            LensSettings lens = virtualCamera.m_Lens;
            if (debug.SceneCamera.orthographic)
                lens.OrthographicSize = DrawSlider("正交范围（越小越近）", lens.OrthographicSize, 0.1f, 50f);
            else
                lens.FieldOfView = DrawSlider("视野 FOV（越小越近）", lens.FieldOfView, 10f, 120f);
            virtualCamera.m_Lens = lens;

            if (GUILayout.Button("恢复原始镜头")) debug.ResetCamera();
        }

        private float DrawSlider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(value.ToString("0.00"), valueStyle);
            GUILayout.EndHorizontal();
            // 场景原值超出常用范围时也保留，避免仅打开面板就改变镜头。
            return GUILayout.HorizontalSlider(value, Mathf.Min(min, value), Mathf.Max(max, value));
        }

        private static string DescribePlayerSpeed(DebugManager debug)
        {
            PlayerEntity player = debug.Player;
            if (player == null) return "当前场景没有找到 PlayerEntity";

            float speed = player.Speed * debug.MoveSpeedMultiplier;
            return "玩家基础速度 " + player.Speed.ToString("0.0") + " → 实际 " + speed.ToString("0.0") + " 世界单位/秒";
        }

        private void DrawHint()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 260f, 24f));
            GUILayout.Label("按 F1 打开开发者调试面板", hintStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (windowStyle != null) return;

            windowStyle = new GUIStyle(GUI.skin.window)
            {
                font = chineseFont,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(12, 12, 28, 12)
            };
            valueStyle = new GUIStyle(GUI.skin.label)
            {
                font = chineseFont,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = chineseFont,
                fontSize = 15
            };
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                font = chineseFont,
                fontSize = 15
            };
            hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.45f);
        }
    }
}
