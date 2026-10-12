using UnityEngine;

namespace Untitled
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CaveBlockoutPlayer : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 5f;

        private CharacterController controller;
        private InputActions inputActions;
        private float verticalSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            inputActions = new InputActions();
        }

        private void OnEnable()
        {
            inputActions.Player.Move.Enable();
        }

        private void OnDisable()
        {
            inputActions.Player.Move.Disable();
            verticalSpeed = 0f;
        }

        private void OnDestroy()
        {
            inputActions.Dispose();
        }

        private void Update()
        {
            if (!controller.enabled) return;

            Vector2 input = Vector2.ClampMagnitude(inputActions.Player.Move.ReadValue<Vector2>(), 1f);
            verticalSpeed = controller.isGrounded && verticalSpeed < 0f
                ? -2f
                : Mathf.Max(verticalSpeed + Physics.gravity.y * Time.deltaTime, -30f);

            // Input Y maps to world Z; Move receives this frame's displacement.
            Vector3 velocity = new Vector3(input.x * moveSpeed, verticalSpeed, input.y * moveSpeed);
            CollisionFlags collisions = controller.Move(velocity * Time.deltaTime);
            if ((collisions & CollisionFlags.Below) != 0) verticalSpeed = -2f;
            else if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
        }
    }
}
