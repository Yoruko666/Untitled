using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        // Preserve serialized prefab values during migration.
        [SerializeField] private string assetId, displayName, category;
        [SerializeField, TextArea] private string implementation;
        [SerializeField] private Texture2D[] referenceImages;
        [SerializeField, TextArea] private string sourceFiles;
        [SerializeField] private Vector3 defaultSize = Vector3.one;
        [SerializeField] private bool wallMounted;
        [SerializeField, Min(0.001f)] private float wallClearance = 0.025f;
        [SerializeField] private Transform visual;
        [SerializeField] private PixelFrame[] frames;
        [FormerlySerializedAs("directionOffset")]
        [SerializeField, Range(0, 7), Tooltip("固定显示的方向帧：0 为正面，1–7 为其他方向；不随摄像机自动切换。")] private int directionFrame;
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
        public int DirectionFrame => Mathf.Clamp(directionFrame, 0, 7);
        public PixelFrame[] Frames => frames;
        public int CurrentFrame { get; private set; }
        public bool IsPixel => frames != null && frames.Length > 0;
        public bool HasEightDirections => frames != null && frames.Length == 8;

        private void OnEnable()
        {
            ApplyVisual();
        }

        private void LateUpdate()
        {
            ApplyVisual();
        }

        public void ApplyVisual()
        {
            if (!IsPixel || visual == null) return;
            // Keep the card aligned with the root; cameras never affect rotation or frame selection.
            visual.localRotation = Quaternion.identity;
            CurrentFrame = HasEightDirections ? DirectionFrame : 0;
            PixelFrame frame = frames[CurrentFrame];
            visual.localScale = new Vector3(frame.ScaleX, frame.ScaleY, 1f);
            visual.localPosition = wallMounted
                ? Vector3.back * Mathf.Max(0.001f, wallClearance)
                : Vector3.zero;
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
