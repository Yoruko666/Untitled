using System;
using UnityEngine;

namespace Untitled
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PlayerRunAnimation : MonoBehaviour
    {
        [Serializable]
        private struct Frame
        {
            // Texture pixels: x, y, width, height, measured from the bottom left.
            public Vector4 Region;
            public Vector2 Pivot;
        }

        [SerializeField] private CharacterController movementSource;
        [SerializeField, Range(1f, 30f)] private float framesPerSecond = 10f;
        [SerializeField, Min(1f)] private float pixelsPerUnit = 460f;
        [SerializeField, Min(0.01f)] private float movementThreshold = 0.1f;
        [Tooltip("Run frames in playback order; the final frame is the standing pose.")]
        [SerializeField] private Frame[] frames;

        private readonly Vector3[] vertices = new Vector3[4];
        private readonly Vector2[] uvs = new Vector2[4];
        private MeshFilter meshFilter;
        private Mesh originalMesh;
        private Mesh animationMesh;
        private Vector2 textureSize;
        private float cycleTime;
        private int displayedFrame = -1;
        private bool facingLeft;
        private bool displayedFacingLeft;
        private bool wasRunning;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            Material material = GetComponent<MeshRenderer>().sharedMaterial;
            Texture texture = material != null ? material.GetTexture("_BaseMap") : null;
            originalMesh = meshFilter.sharedMesh;
            if (movementSource == null || originalMesh == null || originalMesh.vertexCount != 4
                || texture == null || frames == null || frames.Length < 2)
            {
                Debug.LogError("PlayerRunAnimation needs a movement source, a four-vertex mesh, "
                    + "a URP base texture, run frames and a final idle frame.", this);
                enabled = false;
                return;
            }

            textureSize = new Vector2(texture.width, texture.height);
            animationMesh = Instantiate(originalMesh);
            animationMesh.name = "Player Run Animation (Instance)";
            animationMesh.MarkDynamic();
        }

        private void OnEnable()
        {
            if (animationMesh == null) return;

            meshFilter.sharedMesh = animationMesh;
            cycleTime = 0f;
            wasRunning = false;
            displayedFrame = -1;
            ShowFrame(frames.Length - 1);
        }

        private void LateUpdate()
        {
            Vector3 velocity = movementSource.velocity;
            velocity.y = 0f;
            float threshold = Mathf.Max(0.01f, movementThreshold);
            bool running = movementSource.enabled && velocity.sqrMagnitude > threshold * threshold;
            if (!running)
            {
                cycleTime = 0f;
                wasRunning = false;
                ShowFrame(frames.Length - 1);
                return;
            }

            // Billboard updates first, so right/left is relative to the visible camera.
            float sidewaysSpeed = Vector3.Dot(velocity, transform.right);
            if (Mathf.Abs(sidewaysSpeed) > threshold) facingLeft = sidewaysSpeed < 0f;

            int runFrameCount = frames.Length - 1;
            float rate = Mathf.Max(1f, framesPerSecond);
            cycleTime = wasRunning
                ? Mathf.Repeat(cycleTime + Time.deltaTime, runFrameCount / rate)
                : 0f;
            wasRunning = true;
            ShowFrame(Mathf.Min(Mathf.FloorToInt(cycleTime * rate), runFrameCount - 1));
        }

        private void ShowFrame(int index)
        {
            if (displayedFrame == index && displayedFacingLeft == facingLeft) return;

            Frame frame = frames[index];
            float unit = Mathf.Max(1f, pixelsPerUnit);
            float left = -frame.Pivot.x / unit;
            float right = (frame.Region.z - frame.Pivot.x) / unit;
            float bottom = -frame.Pivot.y / unit;
            float top = (frame.Region.w - frame.Pivot.y) / unit;
            float uLeft = frame.Region.x / textureSize.x;
            float uRight = (frame.Region.x + frame.Region.z) / textureSize.x;
            float vBottom = frame.Region.y / textureSize.y;
            float vTop = (frame.Region.y + frame.Region.w) / textureSize.y;
            if (facingLeft)
            {
                (left, right) = (-right, -left);
                (uLeft, uRight) = (uRight, uLeft);
            }

            vertices[0] = new Vector3(left, bottom, 0f);
            vertices[1] = new Vector3(right, bottom, 0f);
            vertices[2] = new Vector3(left, top, 0f);
            vertices[3] = new Vector3(right, top, 0f);
            uvs[0] = new Vector2(uLeft, vBottom);
            uvs[1] = new Vector2(uRight, vBottom);
            uvs[2] = new Vector2(uLeft, vTop);
            uvs[3] = new Vector2(uRight, vTop);
            animationMesh.vertices = vertices;
            animationMesh.uv = uvs;
            animationMesh.RecalculateBounds();
            displayedFrame = index;
            displayedFacingLeft = facingLeft;
        }

        private void OnDisable()
        {
            if (meshFilter != null && animationMesh != null) meshFilter.sharedMesh = originalMesh;
        }

        private void OnDestroy()
        {
            if (animationMesh != null) Destroy(animationMesh);
        }
    }
}
