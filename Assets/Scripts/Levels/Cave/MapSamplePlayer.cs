using UnityEngine;

namespace Untitled
{
    // A local scene sample owns its input without bootstrapping the gameplay managers.
    [RequireComponent(typeof(CharacterController))]
    public sealed class MapSamplePlayer : MonoBehaviour
    {
        [SerializeField] private Camera sampleCamera;
        [SerializeField] private Transform sampleRoot;
        [SerializeField] private Vector3 spawnPoint;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 12f, -20f);
        [SerializeField] private Vector2 minCoord = new Vector2(-55f, -34f);
        [SerializeField] private Vector2 maxCoord = new Vector2(56f, 35f);
        [SerializeField] private float speed = 7.625f;
        [SerializeField] private float resetBelowHeight = 18.25f;
        private CharacterController controller;
        private InputActions inputActions;
        private float verticalSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (sampleCamera == null) sampleCamera = Camera.main;
            inputActions = new InputActions();
        }

        private void OnEnable() => inputActions.Player.Move.Enable();
        private void OnDisable()
        {
            if (inputActions != null) inputActions.Player.Move.Disable();
        }

        private void OnDestroy() => inputActions?.Dispose();

        private void Update()
        {
            Vector2 input = Vector2.ClampMagnitude(inputActions.Player.Move.ReadValue<Vector2>(), 1f);
            Vector3 position = transform.position;
            Vector3 next = position + new Vector3(input.x, 0f, input.y) * (speed * Time.deltaTime);
            Vector3 localNext = sampleRoot.InverseTransformPoint(next);
            localNext.x = Mathf.Clamp(localNext.x, minCoord.x, maxCoord.x);
            localNext.z = Mathf.Clamp(localNext.z, minCoord.y, maxCoord.y);
            next = sampleRoot.TransformPoint(localNext);
            verticalSpeed = controller.isGrounded && verticalSpeed < 0f
                ? -2f : verticalSpeed + Physics.gravity.y * Time.deltaTime;
            controller.Move(new Vector3(next.x - position.x, verticalSpeed * Time.deltaTime, next.z - position.z));
            if (sampleRoot.InverseTransformPoint(transform.position).y < resetBelowHeight)
            {
                controller.enabled = false;
                transform.position = sampleRoot.TransformPoint(spawnPoint);
                verticalSpeed = 0f;
                controller.enabled = true;
            }
        }

        private void LateUpdate()
        {
            if (sampleCamera == null) return;
            Vector3 target = transform.position + Vector3.up;
            sampleCamera.transform.SetPositionAndRotation(target + cameraOffset, Quaternion.LookRotation(-cameraOffset));
        }
    }
}
