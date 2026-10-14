using UnityEngine;

namespace Untitled
{
    /// <summary>Adventure scene references, fall recovery and development-only zone navigation.</summary>
    public sealed class AdventureCaveSession : MonoBehaviour
    {
        [SerializeField] private PlayerEntity player;
        [SerializeField] private CameraController followCamera;
        [SerializeField] private Transform[] zoneMarkers;
        [SerializeField] private string[] zoneNames;
        [SerializeField] private float recoveryHeight = -5.5f;
        private bool showDebug;
        private int recoveryCount;
        private Font debugFont;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle windowStyle;
        private Rect debugWindow = new Rect(12, 90, 290, 510);

        public PlayerEntity Player => player;
        public CameraController FollowCamera => followCamera;
        public Transform[] ZoneMarkers => zoneMarkers;
        public int RecoveryCount => recoveryCount;

        private void Awake()
        {
            player.SetMovementCamera(followCamera.transform);
            followCamera.SetTarget(player.transform);
            if (Debug.isDebugBuild) debugFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 16);
        }

        private void Start()
        {
            TeleportToZone(0);
        }

        private void Update()
        {
            if (player.transform.position.y >= recoveryHeight) return;
            recoveryCount++;
            TeleportToZone(0);
        }

        public void TeleportToZone(int index)
        {
            if (index < 0 || index >= zoneMarkers.Length || zoneMarkers[index] == null) return;
            player.Teleport(zoneMarkers[index].position + Vector3.up * 0.15f);
            Camera camera = followCamera.GetComponent<Camera>();
            Vector3 offset = followCamera.FocusOffset
                + Quaternion.Euler(followCamera.ViewAngle, followCamera.YawAngle, 0f)
                * Vector3.back * followCamera.Distance;
            camera.transform.SetPositionAndRotation(player.transform.position + offset,
                Quaternion.Euler(followCamera.ViewAngle, followCamera.YawAngle, 0f));
        }

        private void OnGUI()
        {
            if (!Debug.isDebugBuild) return;
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { font = debugFont };
                buttonStyle = new GUIStyle(GUI.skin.button) { font = debugFont };
                windowStyle = new GUIStyle(GUI.skin.window) { font = debugFont };
            }
            if (GUI.Button(new Rect(12, Screen.height - 42, 140, 30), "Cave Debug", buttonStyle)) showDebug = !showDebug;
            if (showDebug) debugWindow = GUI.Window(GetInstanceID(), debugWindow, DrawDebug, "Adventure Cave", windowStyle);
        }

        private void DrawDebug(int id)
        {
            GUILayout.Label($"Position: {player.transform.position:F2}", labelStyle);
            GUILayout.Label($"Grounded: {player.Controller.isGrounded} | Recovery: {recoveryCount}", labelStyle);
            GUILayout.Label("区域定位 / 回入口", labelStyle);
            for (int i = 0; i < zoneMarkers.Length; i++)
            {
                if (GUILayout.Button($"{i + 1:00}  {zoneNames[i]}", buttonStyle, GUILayout.Height(23))) TeleportToZone(i);
            }
            GUI.DragWindow(new Rect(0, 0, 290, 20));
        }

        private void OnDestroy()
        {
            if (debugFont != null) Destroy(debugFont);
        }
    }
}
