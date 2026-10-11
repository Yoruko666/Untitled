using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Untitled
{
    [Serializable]
    public struct PixelFrame
    {
        [SerializeField] private float x, y, w, h, sx, sy;

        public Vector4 UV => new Vector4(x, y, w, h);
        public float ScaleX => sx > 0f ? sx : 1f;
        public float ScaleY => sy > 0f ? sy : 1f;
    }

    [DisallowMultipleComponent, ExecuteAlways]
    public sealed class HybridProp : MonoBehaviour
    {
        // Preserve serialized field names and prefab values during migration.
        [SerializeField] private string assetId, displayName, category;
        [SerializeField, TextArea] private string implementation;
        [SerializeField] private Texture2D[] referenceImages;
        [SerializeField, TextArea] private string sourceFiles;
        [SerializeField] private Vector3 defaultSize = Vector3.one;
        [SerializeField] private bool wallMounted;
        [SerializeField, Min(0.001f)] private float wallClearance = 0.025f;
        [SerializeField] private Transform visual;
        [SerializeField, Tooltip("开启时朝向摄像机；关闭时保持根节点朝向，并固定方向帧。")] private bool faceCamera;
        [SerializeField] private PixelFrame[] frames;
        [SerializeField, Range(0, 7)] private int directionOffset;
        private MeshRenderer spriteRenderer;
        private MaterialPropertyBlock block;
        private static readonly int frameRectId = Shader.PropertyToID("_FrameRect");

        public string AssetId => assetId;
        public string DisplayName => displayName;
        public string Category => category;
        public string Implementation => implementation;
        public Texture2D[] ReferenceImages => referenceImages;
        public string SourceFiles => sourceFiles;
        public Vector3 DefaultSize => defaultSize;
        public bool WallMounted => wallMounted;
        public float WallClearance => wallClearance;
        public Transform Visual => visual;
        public bool FaceCamera => faceCamera;
        public PixelFrame[] Frames => frames;
        public int CurrentFrame { get; private set; }
        public bool IsPixel => frames != null && frames.Length > 0;
        public bool HasEightDirections => frames != null && frames.Length == 8;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            ApplyCamera(Camera.main);
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
        }

        private void LateUpdate()
        {
            if (Application.isPlaying) ApplyCamera(Camera.main);
        }

        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera != null && camera.cameraType != CameraType.Reflection) ApplyCamera(camera);
        }

        public void ApplyCamera(Camera camera)
        {
            if (!IsPixel || visual == null) return;
            if (faceCamera)
            {
                Vector3 towards = camera != null
                    ? (camera.orthographic ? -camera.transform.forward : camera.transform.position - transform.position)
                    : -transform.forward;
                towards.y = 0f;
                if (towards.sqrMagnitude < 0.0001f) return;
                // Geometry faces -Z. Only the visual rotates; root rotation selects direction frames.
                visual.rotation = Quaternion.LookRotation(-towards.normalized, Vector3.up);
                Vector3 local = Quaternion.Inverse(transform.rotation) * towards.normalized;
                float angle = Mathf.Atan2(-local.x, -local.z) * Mathf.Rad2Deg;
                CurrentFrame = HasEightDirections
                    ? ((Mathf.RoundToInt(angle / 45f) + directionOffset) % 8 + 8) % 8
                    : 0;
            }
            else
            {
                visual.localRotation = Quaternion.identity;
                CurrentFrame = HasEightDirections ? directionOffset : 0;
            }
            PixelFrame frame = frames[CurrentFrame];
            visual.localScale = new Vector3(frame.ScaleX, frame.ScaleY, 1f);
            if (wallMounted)
            {
                Vector3 rightInRoot = Quaternion.Inverse(transform.rotation) * visual.right;
                float span = defaultSize.x * visual.localScale.x * 0.5f * Mathf.Abs(rightInRoot.z);
                visual.localPosition = Vector3.back * (Mathf.Max(0.001f, wallClearance) + span);
            }
            else visual.localPosition = Vector3.zero;
            if (spriteRenderer == null) spriteRenderer = visual.GetComponent<MeshRenderer>();
            if (spriteRenderer == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(block);
            block.SetVector(frameRectId, frame.UV);
            spriteRenderer.SetPropertyBlock(block);
        }

        public Bounds WorldBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool first = true;
            foreach (Renderer renderer in renderers)
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        [ContextMenu("恢复默认大小")]
        public void ResetScale() => transform.localScale = Vector3.one;
    }
}
