#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNTITLED_DEVELOPER_TOOLS)
using Cinemachine;
using UnityEngine;

namespace Untitled
{
    /// <summary>
    /// 开发者调试工具：集中保存调试参数与面板开关状态，由 Bootstrap 初始化。
    /// 调试值只存在于运行时，不写回预制体或配置资产。
    /// </summary>
    public class DebugManager : ManagerBase<DebugManager>
    {
        public const float MoveSpeedMultiplierMin = 0.1f;
        public const float MoveSpeedMultiplierMax = 10f;

        private const float MoveSpeedMultiplierDefault = 1f;

        private float moveSpeedMultiplier = MoveSpeedMultiplierDefault;
        private DebugOverlay overlay;
        private Camera sceneCamera;
        private float originalViewAngle;
        private float originalYawAngle;
        private float originalDistance;
        private Vector3 originalFocusOffset;
        private float originalOrthographicSize;
        private float originalFieldOfView;
        private Quaternion originalVirtualRotation;
        private LensSettings originalVirtualLens;
        private float originalVirtualDistance;
        private Vector3 originalVirtualOffset;

        /// <summary>调试面板是否打开。</summary>
        public bool IsPanelOpen { get; private set; }

        /// <summary>玩家移速倍数，1 为原始速度，超出范围会被夹取。</summary>
        public float MoveSpeedMultiplier
        {
            get => moveSpeedMultiplier;
            set => moveSpeedMultiplier = Mathf.Clamp(value, MoveSpeedMultiplierMin, MoveSpeedMultiplierMax);
        }

        /// <summary>当前场景的玩家，打开面板时重新查找，用于显示实时速度。</summary>
        public PlayerEntity Player { get; private set; }

        public Camera SceneCamera => sceneCamera;

        public CameraController CameraController { get; private set; }

        public CinemachineVirtualCamera VirtualCamera { get; private set; }

        public CinemachineFramingTransposer FramingTransposer { get; private set; }

        public bool IsCinemachineControlled { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            // 正式构建不创建调试面板；编辑器 Play Mode 与 Development Build 中为 true
            if (Debug.isDebugBuild) CreateOverlay();
        }

        public void OpenPanel()
        {
            IsPanelOpen = true;
            Player = UnityEngine.Object.FindObjectOfType<PlayerEntity>();
            RefreshCamera();
        }

        public void ClosePanel()
        {
            IsPanelOpen = false;
        }

        public void TogglePanel()
        {
            if (IsPanelOpen) ClosePanel();
            else OpenPanel();
        }

        /// <summary>按步长增减移速倍数，超出范围会被夹取。</summary>
        public void AddMoveSpeedMultiplier(float delta)
        {
            MoveSpeedMultiplier += delta;
        }

        /// <summary>恢复移速倍数默认值。</summary>
        public void ResetMoveSpeedMultiplier()
        {
            MoveSpeedMultiplier = MoveSpeedMultiplierDefault;
        }

        /// <summary>打开面板时绑定主摄像头；切换场景后重新记录该镜头的初始参数。</summary>
        private void RefreshCamera()
        {
            Camera currentCamera = Camera.main;
            CinemachineBrain brain = currentCamera != null ? currentCamera.GetComponent<CinemachineBrain>() : null;
            bool usesCinemachine = brain != null && brain.isActiveAndEnabled;
            CinemachineVirtualCamera currentVirtualCamera = usesCinemachine
                ? brain.ActiveVirtualCamera as CinemachineVirtualCamera : null;
            if (currentCamera == sceneCamera && currentVirtualCamera == VirtualCamera
                && usesCinemachine == IsCinemachineControlled) return;

            sceneCamera = currentCamera;
            IsCinemachineControlled = usesCinemachine;
            VirtualCamera = currentVirtualCamera;
            FramingTransposer = VirtualCamera != null
                ? VirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>() : null;
            CameraController = sceneCamera != null ? sceneCamera.GetComponent<CameraController>() : null;
            if (sceneCamera == null) return;

            originalOrthographicSize = sceneCamera.orthographicSize;
            originalFieldOfView = sceneCamera.fieldOfView;
            if (VirtualCamera != null)
            {
                originalVirtualRotation = VirtualCamera.transform.rotation;
                originalVirtualLens = VirtualCamera.m_Lens;
                if (FramingTransposer != null)
                {
                    originalVirtualDistance = FramingTransposer.m_CameraDistance;
                    originalVirtualOffset = FramingTransposer.m_TrackedObjectOffset;
                }
            }
            if (CameraController == null) return;

            originalViewAngle = CameraController.ViewAngle;
            originalYawAngle = CameraController.YawAngle;
            originalDistance = CameraController.Distance;
            originalFocusOffset = CameraController.FocusOffset;
        }

        /// <summary>恢复本次场景首次打开面板时的镜头参数。</summary>
        public void ResetCamera()
        {
            if (sceneCamera == null) return;

            if (IsCinemachineControlled)
            {
                if (VirtualCamera == null) return;
                VirtualCamera.transform.rotation = originalVirtualRotation;
                VirtualCamera.m_Lens = originalVirtualLens;
                if (FramingTransposer != null)
                {
                    FramingTransposer.m_CameraDistance = originalVirtualDistance;
                    FramingTransposer.m_TrackedObjectOffset = originalVirtualOffset;
                }
                return;
            }

            sceneCamera.orthographicSize = originalOrthographicSize;
            sceneCamera.fieldOfView = originalFieldOfView;
            if (CameraController == null) return;

            CameraController.SetViewAngle(originalViewAngle);
            CameraController.SetYawAngle(originalYawAngle);
            CameraController.SetDistance(originalDistance);
            CameraController.SetFocusOffset(originalFocusOffset);
        }

        public void EnsureSceneOverlay() { if (Debug.isDebugBuild) CreateOverlay(); }

        private void CreateOverlay()
        {
            if (overlay != null) return;

            GameObject overlayObject = new("DebugOverlay");
            overlay = overlayObject.AddComponent<DebugOverlay>();
        }
    }
}
#endif
