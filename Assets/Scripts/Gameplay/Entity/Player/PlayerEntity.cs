using UnityEngine;

namespace Untitled
{
    public class PlayerEntity : CharacterBase
    {
        public float Speed = 5;

        [SerializeField] private bool useGravity;
        [SerializeField] private Transform movementCamera;
        private float verticalSpeed;

        protected override void Update()
        {
            Vector2 moveInput = InputManager.Instance.MoveInput;
            float speed = Speed * DebugManager.Instance.MoveSpeedMultiplier;
            if (movementCamera != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(movementCamera.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(movementCamera.right, Vector3.up).normalized;
                Vector3 direction = right * moveInput.x + forward * moveInput.y;
                moveInput = Vector2.ClampMagnitude(new Vector2(direction.x, direction.z), 1f);
            }
            Move(speed * Time.deltaTime * moveInput);
        }

        public void SetMovementCamera(Transform cameraTransform)
        {
            movementCamera = cameraTransform;
        }

        public void Teleport(Vector3 feetPosition)
        {
            bool wasEnabled = Controller.enabled;
            Controller.enabled = false;
            transform.position = feetPosition;
            verticalSpeed = 0f;
            Controller.enabled = wasEnabled;
        }

        protected override void Move(Vector2 moveDelta)
        {
            if (!useGravity)
            {
                base.Move(moveDelta);
                return;
            }

            // Move takes a displacement; gravity must be integrated explicitly.
            verticalSpeed = Controller.isGrounded && verticalSpeed < 0f
                ? -2f
                : verticalSpeed + Physics.gravity.y * Time.deltaTime;
            Controller.Move(new Vector3(moveDelta.x, verticalSpeed * Time.deltaTime, moveDelta.y));
        }
    }
}
