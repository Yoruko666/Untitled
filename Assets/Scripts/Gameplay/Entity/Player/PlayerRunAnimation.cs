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
            // Flight frames use a pivot below the image to keep their feet above the ground.
            public Vector2 Pivot;
        }

        [Serializable]
        private struct DirectionAnimation
        {
            public Material Material;
            [Min(1f)] public float PixelsPerUnit;
            // Frames follow the gait cycle; the final frame is the standing pose.
            public Frame[] Frames;
        }

        private enum Facing
        {
            Right,
            Left,
            Front,
            Back
        }

        [SerializeField] private CharacterController movementSource;
        [Tooltip("Side frames per second; front/back clips share the same full-cycle duration.")]
        [SerializeField, Range(1f, 30f)] private float framesPerSecond = 10f;
        [SerializeField, Min(0.01f)] private float movementThreshold = 0.1f;
        [SerializeField] private DirectionAnimation sideAnimation;
        [SerializeField] private DirectionAnimation frontAnimation;
        [SerializeField] private DirectionAnimation backAnimation;

        private readonly Vector3[] vertices = new Vector3[4];
        private readonly Vector2[] uvs = new Vector2[4];
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh originalMesh;
        private Material originalMaterial;
        private Mesh animationMesh;
        private float cyclePhase;
        private int displayedFrame = -1;
        private Facing facing;
        private Facing displayedFacing;
        private bool wasRunning;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            originalMaterial = meshRenderer.sharedMaterial;
            originalMesh = meshFilter.sharedMesh;
            if (movementSource == null || originalMesh == null || originalMesh.vertexCount != 4
                || !IsValid(sideAnimation) || !IsValid(frontAnimation) || !IsValid(backAnimation))
            {
                Debug.LogError("PlayerRunAnimation needs a movement source, a four-vertex mesh, "
                    + "and side/front/back animations with URP materials, run frames and idle frames.", this);
                enabled = false;
                return;
            }

            animationMesh = Instantiate(originalMesh);
            animationMesh.name = "Player Run Animation (Instance)";
            animationMesh.MarkDynamic();
        }

        private void OnEnable()
        {
            if (animationMesh == null) return;

            meshFilter.sharedMesh = animationMesh;
            cyclePhase = 0f;
            wasRunning = false;
            displayedFrame = -1;
            ShowFrame(GetAnimation().Frames.Length - 1);
        }

        private void LateUpdate()
        {
            if (Time.deltaTime <= 0f) return;

            Vector3 velocity = movementSource.velocity;
            velocity.y = 0f;
            float threshold = Mathf.Max(0.01f, movementThreshold);
            bool running = movementSource.enabled && velocity.sqrMagnitude > threshold * threshold;
            if (!running)
            {
                cyclePhase = 0f;
                wasRunning = false;
                ShowFrame(GetAnimation().Frames.Length - 1);
                return;
            }

            // Billboard updates first. Its horizontal forward points away from the camera.
            Vector3 viewForward = transform.forward;
            viewForward.y = 0f;
            facing = SelectFacing(new Vector2(Vector3.Dot(velocity, transform.right),
                Vector3.Dot(velocity, viewForward.normalized)), facing);

            int runFrameCount = GetAnimation().Frames.Length - 1;
            float rate = Mathf.Max(1f, framesPerSecond);
            float cycleDuration = (sideAnimation.Frames.Length - 1) / rate;
            // Normalized phase keeps the gait cycle continuous across clips with different frame counts.
            cyclePhase = wasRunning
                ? Mathf.Repeat(cyclePhase + Time.deltaTime / cycleDuration, 1f)
                : 0f;
            wasRunning = true;
            ShowFrame(Mathf.Min(Mathf.FloorToInt(cyclePhase * runFrameCount), runFrameCount - 1));
        }

        private static Facing SelectFacing(Vector2 direction, Facing current)
        {
            if (direction.sqrMagnitude < 0.0001f) return current;

            float sideways = Mathf.Abs(direction.x);
            float forward = Mathf.Abs(direction.y);
            bool wasSideways = current == Facing.Right || current == Facing.Left;
            // Keep the current axis near diagonals so small collision jitter does not flicker clips.
            bool useSideways = wasSideways ? sideways * 1.15f >= forward : sideways > forward * 1.15f;
            if (useSideways) return direction.x < 0f ? Facing.Left : Facing.Right;
            return direction.y < 0f ? Facing.Front : Facing.Back;
        }

        private DirectionAnimation GetAnimation()
        {
            switch (facing)
            {
                case Facing.Front: return frontAnimation;
                case Facing.Back: return backAnimation;
                default: return sideAnimation;
            }
        }

        private static bool IsValid(DirectionAnimation animation)
        {
            return animation.Material != null && animation.Material.GetTexture("_BaseMap") != null
                && animation.PixelsPerUnit > 0f && animation.Frames != null && animation.Frames.Length >= 2;
        }

        private void ShowFrame(int index)
        {
            if (displayedFrame == index && displayedFacing == facing) return;

            DirectionAnimation animation = GetAnimation();
            Frame frame = animation.Frames[index];
            Texture texture = animation.Material.GetTexture("_BaseMap");
            Vector2 textureSize = new Vector2(texture.width, texture.height);
            if (displayedFrame < 0 || displayedFacing != facing) meshRenderer.sharedMaterial = animation.Material;
            float unit = animation.PixelsPerUnit;
            float left = -frame.Pivot.x / unit;
            float right = (frame.Region.z - frame.Pivot.x) / unit;
            float bottom = -frame.Pivot.y / unit;
            float top = (frame.Region.w - frame.Pivot.y) / unit;
            float uLeft = frame.Region.x / textureSize.x;
            float uRight = (frame.Region.x + frame.Region.z) / textureSize.x;
            float vBottom = frame.Region.y / textureSize.y;
            float vTop = (frame.Region.y + frame.Region.w) / textureSize.y;
            if (facing == Facing.Left)
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
            displayedFacing = facing;
        }

        private void OnDisable()
        {
            if (animationMesh == null) return;
            meshFilter.sharedMesh = originalMesh;
            meshRenderer.sharedMaterial = originalMaterial;
        }

        private void OnDestroy()
        {
            if (animationMesh != null) Destroy(animationMesh);
        }
    }
}
